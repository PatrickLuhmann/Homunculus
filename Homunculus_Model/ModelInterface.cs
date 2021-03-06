﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homunculus_Model
{
	public interface IHomunculusModel
	{
		void DeleteChallenge(string ChallengeName);
		void LoadDatabase(string Filename);
		void CreateDatabase(string Filename);
		List<Split> CreateChallenge(string ChallengeName, List<string> Splits);
		void ModifyChallenge(string ChallengeName, List<Split> Splits, string NewChallengeName);
		List<string> GetChallenges();
		List<Split> GetSplits(string ChallengeName);
		void StartNewRun(string ChallengeName);
		void Success(string ChallengeName);
		void Failure(string ChallengeName);
		void EndRun(string ChallengeName);
		List<Run> GetRuns(string ChallengeName);
	}
}
