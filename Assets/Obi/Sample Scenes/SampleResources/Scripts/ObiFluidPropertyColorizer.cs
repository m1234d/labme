using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	/**
	 * Sample script that colors fluid particles based on their vorticity (2D only)
	 */
	[RequireComponent(typeof(ObiEmitter))]
	public class ObiFluidPropertyColorizer : MonoBehaviour
	{
		ObiEmitter emitter;

		public Gradient grad;

		void Awake(){
			emitter = GetComponent<ObiEmitter>();
			emitter.OnAddedToSolver += Emitter_OnAddedToSolver;
			emitter.OnRemovedFromSolver += Emitter_OnRemovedFromSolver;
		}

		void Emitter_OnAddedToSolver (object sender, ObiActor.ObiActorSolverArgs e)
		{
			e.Solver.OnFrameEnd += E_Solver_OnFrameEnd;
		}

		void Emitter_OnRemovedFromSolver (object sender, ObiActor.ObiActorSolverArgs e)
		{
			e.Solver.OnFrameEnd -= E_Solver_OnFrameEnd;
		}

		public void OnEnable(){}

		void E_Solver_OnFrameEnd (object sender, EventArgs e)
		{
			if (!isActiveAndEnabled)
				return;

			for (int i = 0; i < emitter.particleIndices.Length; ++i){

				float v = emitter.Solver.userData[emitter.particleIndices[i]][0];

				emitter.colors[i] = grad.Evaluate(v);

			}
		}
	
	}
}

