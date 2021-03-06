﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homunculus_Model
{
	public class ModelXml : IHomunculusModel
	{
		// All data is stored in a single DataSet.
		private DataSet ChallengeRuns;
		private string DatabaseFilename;

		public ModelXml()
		{
			System.Diagnostics.Debug.WriteLine("Enter Model constructor");

			ChallengeRuns = new DataSet("Homunculus");
		}

		public void LoadDatabase(string Filename)
		{
			System.Diagnostics.Debug.WriteLine("Model: Load database file: " + Filename);
			if (Filename == null)
				throw new ArgumentNullException();
			if (!System.IO.File.Exists(Filename))
				throw new ArgumentException();

			// Do some basic validation of the XML.
			// Make sure we throw our own exception.
			string appName = "";
			UInt32 version;
			DataSet tempDS = new DataSet("tempDS");
			try
			{
				// This will throw an exception if it is not a
				// valid XML file.
				//ChallengeRuns.ReadXmlSchema(DatabaseFilename);
				tempDS.ReadXml(Filename);

				// Now check to make sure that this XML is for our app.
				DataRow row;
				row = tempDS.Tables["ConfigParams"].Rows.Find("AppName");
				appName = (string)row["Value"];
				System.Diagnostics.Debug.WriteLine("AppName: " + appName);
				row = tempDS.Tables["ConfigParams"].Rows.Find("SchemaVersion");
				version = UInt32.Parse((string)row["Value"]);
				System.Diagnostics.Debug.WriteLine("SchemaVersion: " + row["Value"]);
			}
			catch
			{
				throw new System.IO.FileFormatException();
			}
			if (appName != "Homunculus" || version != 1)
			{
				throw new System.IO.FileFormatException();
			}

			// Update the Model now that everything is validated.
			ChallengeRuns = tempDS;
			DatabaseFilename = Filename;
			System.Diagnostics.Debug.WriteLine("Valid Homunculus file selected: " + DatabaseFilename);
		}

		public void CreateDatabase(string Filename)
		{
			if (Filename == null)
				throw new ArgumentNullException();
			if (System.IO.File.Exists(Filename))
				throw new ArgumentException();

			// Create the database schema.
			ChallengeRuns = new DataSet("Homunculus");

			// TABLE: Challenges
			DataTable challenges = ChallengeRuns.Tables.Add("Challenges");
			challenges.Columns.Add("ID", typeof(UInt32));
			challenges.PrimaryKey = new DataColumn[] { challenges.Columns["ID"] };
			challenges.Columns["ID"].AutoIncrement = true;
			challenges.Columns["ID"].AutoIncrementSeed = 1;
			challenges.Columns["ID"].AutoIncrementStep = 1;
			challenges.Columns.Add("Name", typeof(string));

			// TABLE: Splits
			DataTable splits = ChallengeRuns.Tables.Add("Splits");
			splits.Columns.Add("ID", typeof(UInt32));
			splits.PrimaryKey = new DataColumn[] { splits.Columns["ID"] };
			splits.Columns["ID"].AutoIncrement = true;
			splits.Columns["ID"].AutoIncrementSeed = 1;
			splits.Columns["ID"].AutoIncrementStep = 1;
			splits.Columns.Add("Name", typeof(string));
			splits.Columns.Add("ChallengeID", typeof(UInt32));
			splits.Columns.Add("IndexWithinChallenge", typeof(UInt32));

			// TABLE: Runs
			DataTable runs = ChallengeRuns.Tables.Add("Runs");
			runs.Columns.Add("ID", typeof(UInt32));
			runs.PrimaryKey = new DataColumn[] { runs.Columns["ID"] };
			runs.Columns["ID"].AutoIncrement = true;
			runs.Columns["ID"].AutoIncrementSeed = 1;
			runs.Columns["ID"].AutoIncrementStep = 1;
			runs.Columns.Add("ChallengeID", typeof(UInt32));
			runs.Columns.Add("CurrentSplit", typeof(int));
			runs.Columns["CurrentSplit"].DefaultValue = 0;
			runs.Columns.Add("Closed", typeof(bool));
			runs.Columns["Closed"].DefaultValue = false;
			runs.Columns.Add("PB", typeof(bool));
			runs.Columns["PB"].DefaultValue = false;

			// TABLE: Counts
			DataTable counts = ChallengeRuns.Tables.Add("Counts");
			counts.Columns.Add("ID", typeof(UInt32));
			counts.PrimaryKey = new DataColumn[] { counts.Columns["ID"] };
			counts.Columns["ID"].AutoIncrement = true;
			counts.Columns["ID"].AutoIncrementSeed = 1;
			counts.Columns["ID"].AutoIncrementStep = 1;
			counts.Columns.Add("RunID", typeof(UInt32));
			counts.Columns.Add("SplitID", typeof(UInt32));
			counts.Columns.Add("Value", typeof(Int32));

			// TABLE: ConfigParams
			DataTable configParameters = ChallengeRuns.Tables.Add("ConfigParams");
			configParameters.Columns.Add("Name", typeof(String));
			configParameters.PrimaryKey = new DataColumn[] { configParameters.Columns["Name"] };
			configParameters.Columns.Add("Value", typeof(String));

			DataRow row;
			row = configParameters.NewRow();
			row["Name"] = "AppName";
			row["Value"] = "Homunculus";
			configParameters.Rows.Add(row);

			row = configParameters.NewRow();
			row["Name"] = "SchemaVersion";
			row["Value"] = "1";
			configParameters.Rows.Add(row);

			// Save the schema to the database file.
			DatabaseFilename = Filename;
			Save();
		}

		/// <summary>
		/// Create a new challenge in the database.
		/// </summary>
		/// <param name="ChallengeName">The name of the challenge (must be unique).</param>
		/// <param name="Splits">Ordered list of the name of the splits for the challenge.</param>
		/// <returns>Ordered list of the splits, with Handle filled in for future use.</returns>
		public List<Split> CreateChallenge(string ChallengeName, List<string> Splits)
		{
			// Check for bad parameters.
			if (ChallengeName == null || Splits == null)
				throw new ArgumentNullException();

			if (Splits.Count == 0)
				throw new ArgumentException();

			// Check for duplicate challenge name.
			if (ChallengeRuns.Tables["Challenges"]
			                 .Select("Name = '" + ChallengeName + "'")
			                 .Length != 0)
				throw new ArgumentException();
			
			// Create a new entry in the Challenges table for the challenge.
			DataTable Challenges = ChallengeRuns.Tables["Challenges"];
			DataRow row = Challenges.NewRow();
			row["Name"] = ChallengeName;
			UInt32 challengeID = Convert.ToUInt32(row["ID"].ToString());
			Challenges.Rows.Add(row);

			// Create the split entries in the Splits table.
			List<Split> outSplits = new List<Split>();
			DataTable dt = ChallengeRuns.Tables["Splits"];
			DataRow dr;
			for (int i = 0; i < Splits.Count; i++)
			{
				dr = dt.NewRow();
				dr["Name"] = Splits[i];
				dr["ChallengeID"] = challengeID;
				dr["IndexWithinChallenge"] = i;
				dt.Rows.Add(dr);
				// Add to the list to return to the caller.
				outSplits.Add(new Split { Name = Splits[i], Handle = Convert.ToUInt32(dr["ID"]) });
			}

			Save();

			return outSplits;
		}

		/// <summary>
		/// Modifies the specified challenge with the provided splits.
		/// </summary>
		/// <param name="ChallengeName"></param>
		/// <param name="NewSplits"></param>
		public void ModifyChallenge(string ChallengeName, List<Split> NewSplits, string NewChallengeName)
		{
			// Check for bad parameters.
			if (ChallengeName == null || (NewSplits == null && NewChallengeName == null))
				throw new ArgumentNullException();

			if (NewSplits != null && NewSplits.Count == 0)
				throw new ArgumentException();

			DataRow[] drChallenge = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (drChallenge.Count() == 0)
				throw new ArgumentException();

			// Make sure there isn't already an active run for this challenge.
			DataRow[] runRows = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + drChallenge[0]["ID"].ToString(),
				"ID DESC");
			if (runRows.Length != 0)
			{
				if (Convert.ToBoolean(runRows[0]["Closed"]) == false)
					throw new InvalidOperationException();
			}
			
			// Process challenge name change.
			if (NewChallengeName != null && NewChallengeName != ChallengeName)
			{
				// Check for duplicate challenge name.
				if (ChallengeRuns.Tables["Challenges"]
								 .Select("Name = '" + NewChallengeName + "'")
								 .Length != 0)
					throw new ArgumentException();

				drChallenge[0]["Name"] = NewChallengeName;
			}

			// Process split name changes and additions.
			if (NewSplits != null)
			{
				for (int i = 0; i < NewSplits.Count; i++)
				{
					Split s = NewSplits[i];

					// See if this split is in the database.
					DataRow[] drSplit = ChallengeRuns.Tables["Splits"]
					.Select("ID = " + s.Handle.ToString());
					if (drSplit.Count() == 1)
					{
						// If so, sets its Name.
						// NOTE: Assumes there is no point in checking to see if the
						// Name is actually different.
						drSplit[0]["Name"] = s.Name;
						// Index might also be different.
						drSplit[0]["IndexWithinChallenge"] = i;
					}
					else
					{
						// If not, it is new, so add it.
						DataRow dr = ChallengeRuns.Tables["Splits"].NewRow();
						dr["Name"] = s.Name;
						dr["ChallengeID"] = drChallenge[0]["ID"];
						dr["IndexWithinChallenge"] = i;
						ChallengeRuns.Tables["Splits"].Rows.Add(dr);
					}
				}
			}

			Save();
		}

		/// <summary>
		/// Deletes the specified challenge from the database.
		/// </summary>
		/// <param name="ChallengeName">The name of the challenge</param>
		public void DeleteChallenge(string ChallengeName)
		{
			// Check for bad parameter.
			if (ChallengeName == null)
				throw new ArgumentNullException();

			DataRow[] drChallenge = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (drChallenge.Count() == 0)
				throw new ArgumentException();

			// TODO: Verify that only one row is returned?

			UInt32 challengeID = Convert.ToUInt32(drChallenge[0]["ID"]);

			// Get the splits that go with this challenge.
			DataRow[] drSplits = ChallengeRuns.Tables["Splits"]
				.Select("ChallengeID = " + challengeID.ToString(), "IndexWithinChallenge ASC");

			// Get the runs that go with this challenge.
			DataRow[] drRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString());

			// Delete the counts associated with each run.
			// Then delete the run itself.
			foreach (DataRow dr in drRuns)
			{
				// Get the counts that go with this run.
				DataRow[] drCounts = ChallengeRuns.Tables["Counts"]
					.Select("RunID = " + dr["ID"].ToString());

				foreach (DataRow row in drCounts)
					row.Delete();

				dr.Delete();
			}

			// Delete the splits.
			foreach (DataRow dr in drSplits)
				dr.Delete();

			// Finally, delete the challenge.
			drChallenge[0].Delete();

			Save();
		}

		private void Save()
		{
			// Make sure there are no outstanding changes.
			ChallengeRuns.AcceptChanges();

			// Save the data to the database file.
			ChallengeRuns.WriteXml(DatabaseFilename, XmlWriteMode.WriteSchema);
			System.Diagnostics.Debug.WriteLine("Model: data file saved - " + DatabaseFilename);
		}

		/// <summary>
		/// Get a list of the challenges in the database.
		/// </summary>
		/// <returns>List of the challenge names (no particular order).</returns>
		public List<string> GetChallenges()
		{
			List<string> challenges = new List<string>();
			foreach (DataRow row in ChallengeRuns.Tables["Challenges"].Rows)
			{
				challenges.Add(row["Name"].ToString());
			}
			return challenges;
		}

		/// <summary>
		/// Get an ordered list of the splits in the given challenge.
		/// </summary>
		/// <param name="ChallengeName">Name of the challenge.</param>
		/// <returns>Ordered list of the splits in the given challenge.</returns>
		public List<Split> GetSplits(string ChallengeName)
		{
			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
										.Select("Name = '" + ChallengeName + "'");

			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Get the splits that go with this challenge.
			dr = ChallengeRuns.Tables["Splits"]
							  .Select("ChallengeID = " + challengeID.ToString(),
									  "IndexWithinChallenge ASC");

			List<Split> splits = new List<Split>();
			foreach (var r in dr)
			{
				splits.Add(new Split
				{
					Name = r["Name"].ToString(),
					Handle = Convert.ToUInt32(r["ID"])
				});
			}

			return splits;
		}

		public void StartNewRun(string ChallengeName)
		{
			if (ChallengeName == null)
				throw new ArgumentNullException();

			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
										.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (dr.Count() == 0)
				throw new ArgumentException();

			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Make sure there isn't already an active run for this challenge.
			DataRow[] runRows = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString(),
				"ID DESC");
			if (runRows.Length != 0)
			{
				if (Convert.ToBoolean(runRows[0]["Closed"]) == false)
					throw new InvalidOperationException();
			}

			// Create a new Run for this Challenge.
			DataRow runRow = ChallengeRuns.Tables["Runs"].NewRow();
			runRow["ChallengeID"] = challengeID;
			UInt32 runId = Convert.ToUInt32(runRow["ID"].ToString());
			ChallengeRuns.Tables["Runs"].Rows.Add(runRow);

			//
			// Create the Counts for this new Run.
			//

			// Get the splits that go with this challenge.
			dr = ChallengeRuns.Tables["Splits"]
							  .Select("ChallengeID = " + challengeID.ToString(),
									  "IndexWithinChallenge ASC");

			// For each split, create a row in the Counts table.
			foreach (var sr in dr)
			{
				// Create the new count row.
				DataRow countRow = ChallengeRuns.Tables["Counts"].NewRow();

				// Set the SplitID.
				UInt32 splitId = Convert.ToUInt32(sr["ID"]);
				countRow["SplitID"] = splitId;

				// Set the RunID.
				countRow["RunID"] = runId;

				// Set the default Value.
				countRow["Value"] = 0;

				// Put the row into the table.
				ChallengeRuns.Tables["Counts"].Rows.Add(countRow);
			}

			Save();
		}

		public void Success(string ChallengeName)
		{
			// Check for bad parameters.
			if (ChallengeName == null)
				throw new ArgumentNullException();

			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");
			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Get the splits that go with this challenge.
			dr = ChallengeRuns.Tables["Splits"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"IndexWithinChallenge ASC");
			int numSplits = dr.Count();

			// Get the active run for this challenge. Sorting by ID DESC
			// means the newest will be at index 0.
			DataRow[] rowRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"ID DESC");
			// If the newest run is closed then Success is meaningless.
			if (Convert.ToBoolean(rowRuns[0]["Closed"]) == true)
				throw new InvalidOperationException();
			UInt32 runID = Convert.ToUInt32(rowRuns[0]["ID"]);

			// Increment CurrentSplit.
			int currSplit = (int)rowRuns[0]["CurrentSplit"];
			currSplit++;
			rowRuns[0]["CurrentSplit"] = currSplit;
			// NOTE: Do this here because PB work depends on it.
			ChallengeRuns.Tables["Runs"].AcceptChanges();

			// Check to see if that was the final split.
			if (currSplit == numSplits)
			{
				// This run is now closed.
				rowRuns[0]["Closed"] = true;

				// Check to see if this is a new PB.

				// Calculate total failures for this run.
				int currentTotalFails = CalcTotalFails(runID);

				// Figure out the current PB.
				int pb = GetPB(challengeID);

				if (currentTotalFails < pb || pb == -1)
				{
					// Clear the old PB run.
					ClearPB(challengeID);

					// New PB! Huzzah!
					rowRuns[0]["PB"] = true;
				}
			}

			Save();
		}

		// NOTE: The caller must update the Runs table.
		private void ClearPB(UInt32 ChallengeID)
		{
			// Get the PB run.
			DataRow[] rowRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + ChallengeID.ToString() + " AND PB = true");

			if (rowRuns.Count() == 1)
			{
				rowRuns[0]["PB"] = false;
			}

			Save();
		}

		private int GetPB(UInt32 ChallengeID)
		{
			int currPb = -1;

			// Get the PB run for the given challenge.
			DataRow[] rowRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + ChallengeID.ToString() + " AND PB = true");

			// There might not be a PB (e.g. no runs are complete).
			if (rowRuns.Count() == 1)
			{
				// Get the counts for this run.
				DataRow[] rowCounts = ChallengeRuns.Tables["Counts"]
					.Select("RunID = " + rowRuns[0]["ID"].ToString());

				// Calculate the count total.
				currPb = 0;
				foreach (var cr in rowCounts)
				{
					int val = (int)cr["Value"];
					currPb += val;
				}
			}

			return currPb;
		}

		private int CalcTotalFails(UInt32 RunID)
		{
			int total = 0;

			DataRow[] countRows = ChallengeRuns.Tables["Counts"]
				.Select("RunID = " + RunID.ToString());

			foreach (var cr in countRows)
			{
				int val = (int)cr["Value"];
				total += val;
			}

			return total;
		}

		public void Failure(string ChallengeName)
		{
			// Check for bad parameters.
			if (ChallengeName == null)
				throw new ArgumentNullException();

			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (dr.Count() == 0)
				throw new ArgumentException();

			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Get the active run for this challenge. Sorting by ID DESC
			// means the newest will be at index 0.
			DataRow[] rowRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"ID DESC");
			// If the newest run is closed then Failure is meaningless.
			if (Convert.ToBoolean(rowRuns[0]["Closed"]) == true)
				throw new InvalidOperationException();
			UInt32 runID = Convert.ToUInt32(rowRuns[0]["ID"]);
			int currentSplit = (int)rowRuns[0]["CurrentSplit"];

			// Get the SplitID of the split we are on.
			dr = ChallengeRuns.Tables["Splits"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"IndexWithinChallenge ASC");
			UInt32 splitID = Convert.ToUInt32(dr[currentSplit]["ID"]);

			// Find the current Count and increment it.
			DataRow[] countRows = ChallengeRuns.Tables["Counts"]
				.Select("RunID = " + runID.ToString() + " AND SplitID = " + splitID.ToString());
			int value = (int)countRows[0]["Value"];
			value++;
			countRows[0]["Value"] = value;

			Save();
		}

		public void EndRun(string ChallengeName)
		{
			if (ChallengeName == null)
				throw new ArgumentNullException();

			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (dr.Count() == 0)
				throw new ArgumentException();

			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Make sure there is an active run for this challenge.
			DataRow[] runRows = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"ID DESC");
			if ((runRows.Length == 0) ||
				(Convert.ToBoolean(runRows[0]["Closed"]) == true))
				throw new InvalidOperationException();

			// Close the run.
			runRows[0]["Closed"] = true;

			Save();
		}

		public List<Run> GetRuns(string ChallengeName)
		{
			if (ChallengeName == null)
				throw new ArgumentNullException();

			// Get the Challenge ID.
			DataRow[] dr = ChallengeRuns.Tables["Challenges"]
				.Select("Name = '" + ChallengeName + "'");

			// Verify that the challenge was in the database.
			if (dr.Count() == 0)
				throw new ArgumentException();

			UInt32 challengeID = Convert.ToUInt32(dr[0]["ID"]);

			// Get the splits that go with this challenge.
			DataRow[] rowSplits = ChallengeRuns.Tables["Splits"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"IndexWithinChallenge ASC");

			// Get the runs that go with this challenge.
			DataRow[] rowRuns = ChallengeRuns.Tables["Runs"]
				.Select("ChallengeID = " + challengeID.ToString(),
					"ID ASC");

			List<Run> runs = new List<Run>();
			foreach (var rr in rowRuns)
			{
				// Need a new list for each Run.
				List<int> runCounts = new List<int>();

				// Get the ID of this run.
				UInt32 runId = Convert.ToUInt32(rr["ID"]);

				// Get

				// The splits need to be ordered for this to work.
				foreach (var sr in rowSplits)
				{
					UInt32 splitId = Convert.ToUInt32(sr["ID"]);
					DataRow[] count = ChallengeRuns.Tables["Counts"]
						.Select("RunID = " + runId + " AND SplitID = " + splitId);

					runCounts.Add(Convert.ToInt32(count[0]["Value"]));
				}

				// Add the newly-created list of split values to the main list.
				runs.Add(new Run
				{
					SplitCounts = runCounts,
					CurrentSplit = (int)rr["CurrentSplit"],
					Closed = (bool)rr["Closed"],
					PB = (bool)rr["PB"]
				});
			}
			return runs;
		}
	}
}
