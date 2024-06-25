using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class FirestoreService
    {
        private FirestoreDb _db;

        public FirestoreService(string projectId, string jsonPath)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);
            _db = FirestoreDb.Create(projectId);
        }

        public async Task<string> GetUserIdByEmailAsync(string collectionPath, string email)
        {
            Query query = _db.Collection(collectionPath).WhereEqualTo("email", email);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                return document.Id; // Zwraca ID dokumentu jako ID użytkownika
            }

            return null;
        }

        public async Task SaveTrainingResultAsync(ExerciseSummary summary, string userId)
        {
            // Tworzenie referencji do dokumentu w Firestore
            DocumentReference docRef = _db.Collection("users").Document(userId).Collection("trainings").Document();

            // Konwersja obiektu ExerciseSummary na mapę
            var summaryMap = new Dictionary<string, object>
            {
                { "Date", summary.Date },
                { "ExerciseDurationMinutes", summary.ExerciseDurationMinutes },
                { "ScorePercentage", summary.ScorePercentage },
                { "JointScores", summary.JointScores }
            };

            // Zapis do Firestore
            await docRef.SetAsync(summaryMap);
        }
    }
}