using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

	[RequireComponent(typeof(ObiEmitter))]
	public class ViscositySurfTensionToUserData : MonoBehaviour
	{
		ObiEmitter emitter;

		void Awake(){
			emitter = GetComponent<ObiEmitter>();
			emitter.OnEmitParticle += Emitter_OnEmitParticle;
		}

		void Emitter_OnEmitParticle (object sender, ObiEmitter.ObiParticleEventArgs e)
		{
			if (emitter.Solver != null){
				int k = emitter.particleIndices[e.index];
				
				Vector4 userData = emitter.Solver.userData[k];
				userData[0] = emitter.Solver.viscosities[k];
				userData[1] = emitter.Solver.surfaceTension[k];
				emitter.Solver.userData[k] = userData;
			}		
		}
	
	}
}

