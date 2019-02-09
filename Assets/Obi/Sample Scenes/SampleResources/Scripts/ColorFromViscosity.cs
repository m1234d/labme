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
	public class ColorFromViscosity : MonoBehaviour
	{
		ObiEmitter emitter;

		public float min = 0;
		public float max = 1;
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

				int k = emitter.particleIndices[i];

				emitter.colors[i] = grad.Evaluate((emitter.Solver.viscosities[k] - min) / (max - min));

				emitter.Solver.viscosities[k] = emitter.Solver.userData[k][0];
				emitter.Solver.surfaceTension[k] = emitter.Solver.userData[k][1];
			}
		}
	
	}
}

