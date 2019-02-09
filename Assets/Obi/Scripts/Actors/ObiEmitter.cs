using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

	[ExecuteInEditMode]
	[AddComponentMenu("Physics/Obi/Obi Emitter")]
	public class ObiEmitter : ObiActor {

		public class ObiParticleEventArgs : System.EventArgs{
			public int index = -1;	/**< particle index.*/
			public ObiParticleEventArgs(int index){
				this.index = index;
			}
		}

		public event System.EventHandler<ObiParticleEventArgs> OnEmitParticle;
		public event System.EventHandler<ObiParticleEventArgs> OnKillParticle;

		public enum EmissionMethod{
			STREAM,		/**< continously emits particles until there are no particles left to emit.*/
			BURST		/**< distributes particles in the surface of the object. Burst emission.*/
		}

		[SerializeProperty("FluidPhase")]
		[SerializeField] private int fluidPhase = 1;

		[SerializeProperty("EmitterMaterial")]
		[SerializeField] private ObiEmitterMaterial emitterMaterial = null;
	
		[Tooltip("Amount of solver particles used by this emitter.")]
		[SerializeProperty("NumParticles")]
		[SerializeField] private int numParticles = 1000;

		[Tooltip("Changes how the emitter behaves. Available modes are Stream and Burst.")]
		public EmissionMethod emissionMethod = EmissionMethod.STREAM;

		[Tooltip("Speed (in units/second) of emitted particles. Setting it to zero will stop emission. Large values will cause more particles to be emitted.")]
		public float speed = 0.25f;

		[Tooltip("Lifespan of each particle.")]
		public float lifespan = 4;

		[Range(0,1)]
		[Tooltip("Amount of randomization applied to particles.")]
		public float randomVelocity = 0;

		[HideInInspector][SerializeField]private List<ObiEmitterShape> emitterShapes = new List<ObiEmitterShape>();
		private IEnumerator<ObiEmitterShape.DistributionPoint> distEnumerator;

		private int activeParticleCount = 0;			/**< number of currently active particles*/
		[HideInInspector] public float[] life;			/**< per particle remaining life in seconds.*/

		private float unemittedBursts = 0;

		public int NumParticles{
			set{
				if (numParticles != value){
					numParticles = value;
					IEnumerator generation = Initialize();
					while (generation.MoveNext());
				}
			}
			get{return numParticles;}
		}

		public int ActiveParticles{
			get{return activeParticleCount;}
		}

		public int FluidPhase{
			set{
				if (fluidPhase != value){
					fluidPhase = value;
					UpdateParticlePhases();
				}
			}
			get{return fluidPhase;}
		}

		public ObiEmitterMaterial EmitterMaterial{
			set{
				if (emitterMaterial != value){

					if (emitterMaterial != null)
					emitterMaterial.OnChangesMade -= EmitterMaterial_OnChangesMade;

					emitterMaterial = value;
				
					if (emitterMaterial != null){
						emitterMaterial.OnChangesMade += EmitterMaterial_OnChangesMade;
						EmitterMaterial_OnChangesMade(emitterMaterial,null);
					}
					
				}
			}
			get{
				return emitterMaterial;
			}
		}

		public override bool UsesCustomExternalForces{ 
			get{return true;}
		}
	
		public void Awake()
		{
			selfCollisions = true;

			UpdateEmitterDistribution();

			distEnumerator = GetDistributionEnumerator();
			IEnumerator generation = Initialize();
			while (generation.MoveNext());
		}

		public override void OnEnable(){

			if (emitterMaterial != null)
				emitterMaterial.OnChangesMade += EmitterMaterial_OnChangesMade;			

			base.OnEnable();

		}
		
		public override void OnDisable(){

			if (emitterMaterial != null)
				emitterMaterial.OnChangesMade -= EmitterMaterial_OnChangesMade;	
			
			base.OnDisable();
			
		}

		public override bool AddToSolver(object info){
			
			if (Initialized && base.AddToSolver(info)){
				solver.OnUpdateParameters += Solver_OnUpdateParameters;
				return true;
			}
			return false;
		}
		
		public override bool RemoveFromSolver(object info){

			if (solver != null)
				solver.OnUpdateParameters -= Solver_OnUpdateParameters;
			return base.RemoveFromSolver(info);

		}

		public void AddShape(ObiEmitterShape shape){
			if (!emitterShapes.Contains(shape)){
				emitterShapes.Add(shape);
				shape.particleSize = (emitterMaterial != null) ? emitterMaterial.GetParticleSize(solver.parameters.mode) : 0.1f;
				shape.GenerateDistribution();
				distEnumerator = GetDistributionEnumerator();
			}
		}

		public void RemoveShape(ObiEmitterShape shape){
			emitterShapes.Remove(shape);
			distEnumerator = GetDistributionEnumerator();
		}

		/**
	 	* Generates the particle based physical representation of the emitter. This is the initialization method for the rope object
		* and should not be called directly once the object has been created.
	 	*/
		protected override IEnumerator Initialize()
		{		
			initialized = false;			
			initializing = true;

			RemoveFromSolver(null);

			active = new bool[numParticles];
			life = new float[numParticles];
			positions = new Vector3[numParticles];
			velocities = new Vector3[numParticles];
			invMasses  = new float[numParticles];
			principalRadii = new Vector3[numParticles];
			phases = new int[numParticles];
			colors = new Color[numParticles];

			activeParticleCount = 0;

			float restDistance = (emitterMaterial != null) ? emitterMaterial.GetParticleSize(solver.parameters.mode) : 0.1f ;
			float pmass = (emitterMaterial != null) ? emitterMaterial.GetParticleMass(solver.parameters.mode) : 0.1f;

			for (int i = 0; i < numParticles; i++){

				active[i] = false;
				life[i] = 0;
				invMasses[i] = 1.0f/pmass;
				positions[i] = Vector3.zero;

				if (emitterMaterial != null && !(emitterMaterial is ObiEmitterMaterialFluid)){
					float randomRadius = UnityEngine.Random.Range(0,restDistance/100.0f * (emitterMaterial as ObiEmitterMaterialGranular).randomness);
					principalRadii[i] = Vector3.one * Mathf.Max(0.001f + restDistance*0.5f - randomRadius);
				}else
					principalRadii[i] = Vector3.one * restDistance*0.5f;

				colors[i] = Color.white;

				phases[i] = Oni.MakePhase(fluidPhase,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) |
											    	   ((emitterMaterial != null && (emitterMaterial is ObiEmitterMaterialFluid))?Oni.ParticlePhase.Fluid:0));

			}

			initializing = false;
			initialized = true;
			
			yield return null;
		}

		private void UpdateEmitterDistribution(){
			for (int i = 0; i < emitterShapes.Count;++i){
				emitterShapes[i].particleSize = (emitterMaterial != null) ? emitterMaterial.GetParticleSize(solver.parameters.mode) : 0.1f;
				emitterShapes[i].GenerateDistribution();
			}
		}

		private IEnumerator<ObiEmitterShape.DistributionPoint> GetDistributionEnumerator(){

			// In case there are no shapes, emit using the emitter itself as a single-point shape.
			if (emitterShapes.Count == 0){
				while (true){
					Matrix4x4 l2sTransform = ActorLocalToSolverMatrix;
					yield return new ObiEmitterShape.DistributionPoint(l2sTransform.GetColumn(3),l2sTransform.GetColumn(2),Color.white);
				}
			}

			// Emit distributing emission among all shapes:
			while (true)
		    {
				for (int j = 0; j < emitterShapes.Count; ++j){
					ObiEmitterShape shape = emitterShapes[j];

					if (shape.distribution.Count == 0) 
						yield return new ObiEmitterShape.DistributionPoint(shape.ShapeLocalToSolverMatrix.GetColumn(3),shape.ShapeLocalToSolverMatrix.GetColumn(2),Color.white);

					for (int i = 0; i < shape.distribution.Count; ++i)
						yield return shape.distribution[i].GetTransformed(shape.ShapeLocalToSolverMatrix,shape.color);
					
				}
			}

		}

		void EmitterMaterial_OnChangesMade (object sender, EventArgs e)
		{
			for (int i = 0; i < activeParticleCount; ++i){
				UpdateParticleMaterial(i);
			}

			UpdateEmitterDistribution();
		}

		void Solver_OnUpdateParameters (object sender, EventArgs e)
		{
			for (int i = 0; i < activeParticleCount; ++i){
				UpdateParticleResolution(i);
			}

			UpdateEmitterDistribution();
		}

		public void ResetParticle(int index, float offset){	

			distEnumerator.MoveNext();
			ObiEmitterShape.DistributionPoint distributionPoint = distEnumerator.Current;

			Vector3 spawnVelocity = Vector3.Lerp(distributionPoint.velocity,UnityEngine.Random.onUnitSphere,randomVelocity);
			Vector3 positionOffset = spawnVelocity * (speed * Time.fixedDeltaTime) * offset;

			solver.positions [particleIndices[index]] = distributionPoint.position + positionOffset;
			solver.velocities[particleIndices[index]] = spawnVelocity * speed;

			UpdateParticleMaterial(index);

			colors[index] = distributionPoint.color;
		}

		public override void UpdateParticlePhases(){

			if (!InSolver) return;

			Oni.ParticlePhase particlePhase = Oni.ParticlePhase.Fluid;
			if (emitterMaterial != null && !(emitterMaterial is ObiEmitterMaterialFluid))
				particlePhase = 0;
	
			for(int i = 0; i < phases.Length; i++){
				phases[i] = Oni.MakePhase(fluidPhase,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) | particlePhase);
			}

			PushDataToSolver(ParticleData.PHASES);
		}

		public void UpdateParticleResolution(int index){

			if (solver == null) return;

			ObiEmitterMaterialFluid fluidMaterial = emitterMaterial as ObiEmitterMaterialFluid;

			int solverIndex = particleIndices[index];

			float restDistance = (emitterMaterial != null) ? emitterMaterial.GetParticleSize(solver.parameters.mode) : 0.1f ;
			float pmass = (emitterMaterial != null) ? emitterMaterial.GetParticleMass(solver.parameters.mode) : 0.1f;

			if (emitterMaterial != null && !(emitterMaterial is ObiEmitterMaterialFluid)){
				float randomRadius = UnityEngine.Random.Range(0,restDistance/100.0f * (emitterMaterial as ObiEmitterMaterialGranular).randomness);
				solver.principalRadii[solverIndex] = Vector3.one * Mathf.Max(0.001f + restDistance*0.5f - randomRadius);
			}else
				solver.principalRadii[solverIndex] = Vector3.one * restDistance*0.5f;

			solver.invMasses[solverIndex] = 1/pmass;
			solver.smoothingRadii[solverIndex] = fluidMaterial != null ? fluidMaterial.GetSmoothingRadius(solver.parameters.mode) : 1f / (10 * Mathf.Pow(1,1/(solver.parameters.mode == Oni.SolverParameters.Mode.Mode3D ? 3.0f : 2.0f)));

		}

		public void UpdateParticleMaterial(int index){

			if (solver == null) return;

			UpdateParticleResolution(index);

			ObiEmitterMaterialFluid fluidMaterial = emitterMaterial as ObiEmitterMaterialFluid;

			int solverIndex = particleIndices[index];

			solver.restDensities[solverIndex] = fluidMaterial != null ? fluidMaterial.restDensity : 0;
			solver.viscosities[solverIndex] = fluidMaterial != null ? fluidMaterial.viscosity : 0;
			solver.surfaceTension[solverIndex] = fluidMaterial != null ? fluidMaterial.surfaceTension : 0;
			solver.buoyancies[solverIndex] = fluidMaterial != null ? fluidMaterial.buoyancy : -1;
			solver.atmosphericDrag[solverIndex] = fluidMaterial != null ? fluidMaterial.atmosphericDrag : 0;
			solver.atmosphericPressure[solverIndex] = fluidMaterial != null ? fluidMaterial.atmosphericPressure : 0;
			solver.diffusion[solverIndex] = fluidMaterial != null ? fluidMaterial.diffusion : 0;
			solver.userData[solverIndex] = fluidMaterial != null ? fluidMaterial.diffusionData : Vector4.zero;

			Oni.ParticlePhase particlePhase = Oni.ParticlePhase.Fluid;
			if (emitterMaterial != null && !(emitterMaterial is ObiEmitterMaterialFluid))
				particlePhase = 0;

			solver.phases[solverIndex] = Oni.MakePhase(fluidPhase,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) | particlePhase);
		}

		/**
		 * Asks the emiter to emits a new particle. Returns whether the emission was succesful.
		 */
		public bool EmitParticle(float offset){

			if (activeParticleCount == numParticles) return false;

			life[activeParticleCount] = lifespan;
			
			// move particle to its spawn position:
			ResetParticle(activeParticleCount, offset);

			// now there's one active particle more:
			active[activeParticleCount] = true;
			activeParticleCount++;

			if (OnEmitParticle != null)
				OnEmitParticle(this,new ObiParticleEventArgs(activeParticleCount-1));

			return true;
		}

		/**
		 * Asks the emiter to kill a particle. Returns whether it was succesful.
		 */
		private bool KillParticle(int index){

			if (activeParticleCount == 0 || index >= activeParticleCount) return false;

			// reduce amount of active particles:
			activeParticleCount--;
			active[activeParticleCount] = false; 

			// swap solver particle indices:
			int temp = solver.particleToActor[particleIndices[activeParticleCount]].indexInActor;
            solver.particleToActor[particleIndices[activeParticleCount]].indexInActor = index;
            solver.particleToActor[particleIndices[index]].indexInActor = temp;

			temp = particleIndices[activeParticleCount];
			particleIndices[activeParticleCount] = particleIndices[index];
			particleIndices[index] = temp;

			// also swap lifespans, so the swapped particle enjoys the rest of its life! :)
			float tempLife = life[activeParticleCount];
			life[activeParticleCount] = life[index];
			life[index] = tempLife;

			// and swap colors:
			Color tempColor = colors[activeParticleCount];
			colors[activeParticleCount] = colors[index];
			colors[index] = tempColor;

			if (OnKillParticle != null)
				OnKillParticle(this,new ObiParticleEventArgs(activeParticleCount));

			return true;
			
		}

		public void KillAll(){

			for (int i = activeParticleCount-1; i >= 0; --i){
				KillParticle(i);
			}

			PushDataToSolver(ParticleData.ACTIVE_STATUS);
		}

		private int GetDistributionPointsCount(){
			int size = 0;
			for (int i = 0; i < emitterShapes.Count;++i)	
				size += emitterShapes[i].distribution.Count;
			return Mathf.Max(1,size);
		}

		public override void OnSolverStepBegin(){

			base.OnSolverStepBegin();

			bool emitted = false;
			bool killed = false;

			// cache a per-shape matrix that transforms from shape local space to solver space.
			for (int j = 0; j < emitterShapes.Count; ++j){
				emitterShapes[j].UpdateLocalToSolverMatrix();
			}

			// Update lifetime and kill dead particles:
			for (int i = activeParticleCount-1; i >= 0; --i){
				life[i] -= Time.deltaTime;

				if (life[i] <= 0){
					killed |= KillParticle(i);	
				}
			}

			int emissionPoints = GetDistributionPointsCount();

			// stream emission:
			if (emissionMethod == EmissionMethod.STREAM)
			{	

				// number of bursts per simulation step:
				float burstCount = (speed * Time.fixedDeltaTime) / ((emitterMaterial != null) ? emitterMaterial.GetParticleSize(solver.parameters.mode) : 0.1f);
	
				// Emit new particles:
				unemittedBursts += burstCount;
				int burst = 0;
				while (unemittedBursts > 0){
					for (int i = 0; i < emissionPoints; ++i){
						emitted |= EmitParticle(burst / burstCount);
					}
					unemittedBursts -= 1;
					burst++;
				}

			}else{ // burst emission:

				if (activeParticleCount == 0){
					for (int i = 0; i < emissionPoints; ++i){
						emitted |= EmitParticle(0);
					}
				}
			}

			// Push active array to solver if any particle has been killed or emitted this frame.
			if (emitted || killed){
				PushDataToSolver(ParticleData.ACTIVE_STATUS);		
			}	

		}
	}
}
