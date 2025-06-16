// See https://aka.ms/new-console-template for more informat ion
using System.Text.Json;



namespace StudyMate
{
    // Data Models
    public class Flashcard
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class Subject
    {
        public string Name { get; set; } = string.Empty;
        public List<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class QuizResult
    {
        public string SubjectName { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<int> WrongAnswerIds { get; set; } = new List<int>();
        public DateTime QuizDate { get; set; } = DateTime.Now;
        public decimal Score => TotalQuestions > 0 ? (decimal)CorrectAnswers / TotalQuestions * 100 : 0;
    }

    public class UserPerformance
    {
        public List<QuizResult> QuizHistory { get; set; } = new List<QuizResult>();
        public Dictionary<string, List<int>> WrongAnswersBySubject { get; set; } = new Dictionary<string, List<int>>();
    }

    // Core Manager Classes
    public class FlashcardManager
    {
        private List<Subject> _subjects = new List<Subject>();
        private readonly string _dataFile = "flashcards.json";

        public FlashcardManager()
        {
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    string jsonString = File.ReadAllText(_dataFile);
                    _subjects = JsonSerializer.Deserialize<List<Subject>>(jsonString) ?? new List<Subject>();
                    Console.WriteLine("Flashcard data loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading flashcard data: {ex.Message}");
                _subjects = new List<Subject>();
            }
        }

        public void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_subjects, options);
                File.WriteAllText(_dataFile, jsonString);
                Console.WriteLine("Flashcard data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving flashcard data: {ex.Message}");
            }
        }

        public List<Subject> GetAllSubjects()
        {
            return _subjects;
        }

        public Subject GetSubjectByName(string name)
        {
            return _subjects.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void AddSubject(string subjectName)
        {
            if (_subjects.Any(s => s.Name.Equals(subjectName, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Subject already exists!");
                return;
            }

            _subjects.Add(new Subject { Name = subjectName });
            SaveData();
            Console.WriteLine($"Subject '{subjectName}' created successfully.");
        }

        public void AddFlashcard(string subjectName, string question, string answer)
        {
            var subject = GetSubjectByName(subjectName);
            if (subject == null)
            {
                Console.WriteLine("Subject not found!");
                return;
            }

            int newId = subject.Flashcards.Count > 0 ? subject.Flashcards.Max(f => f.Id) + 1 : 1;
            subject.Flashcards.Add(new Flashcard
            {
                Id = newId,
                Question = question,
                Answer = answer
            });

            SaveData();
            Console.WriteLine("Flashcard added successfully.");
        }

        public void DeleteFlashcard(string subjectName, int flashcardId)
        {
            var subject = GetSubjectByName(subjectName);
            if (subject == null)
            {
                Console.WriteLine("Subject not found!");
                return;
            }

            var flashcard = subject.Flashcards.FirstOrDefault(f => f.Id == flashcardId);
            if (flashcard == null)
            {
                Console.WriteLine("Flashcard not found!");
                return;
            }

            subject.Flashcards.Remove(flashcard);
            SaveData();
            Console.WriteLine("Flashcard deleted successfully.");
        }
    }

    public class QuizEngine
    {
        private readonly FlashcardManager _flashcardManager;
        private readonly PerformanceTracker _performanceTracker;
        private readonly Random _random = new Random();

        public QuizEngine(FlashcardManager flashcardManager, PerformanceTracker performanceTracker)
        {
            _flashcardManager = flashcardManager;
            _performanceTracker = performanceTracker;
        }

        public void StartQuiz(string subjectName, bool isRetestMode = false)
        {
            var subject = _flashcardManager.GetSubjectByName(subjectName);
            if (subject == null)
            {
                Console.WriteLine("Subject not found!");
                return;
            }

            List<Flashcard> questionsToAsk;

            if (isRetestMode)
            {
                var wrongAns = _performanceTracker.GetWrongAnswers(subjectName);
                if (wrongAns.Count == 0)
                {
                    Console.WriteLine("No wrong answers to retest for this subject!");
                    return;
                }
                questionsToAsk = subject.Flashcards.Where(f => wrongAns.Contains(f.Id)).ToList();
                Console.WriteLine($"\n=== RETEST MODE: {subjectName} ===");
                Console.WriteLine($"Retesting {questionsToAsk.Count} previously incorrect answers.\n");
            }
            else
            {
                questionsToAsk = subject.Flashcards.ToList();
                Console.WriteLine($"\n=== QUIZ: {subjectName} ===");
                Console.WriteLine($"Starting quiz with {questionsToAsk.Count} questions.\n");
            }

            if (questionsToAsk.Count == 0)
            {
                Console.WriteLine("No flashcards available for this subject!");
                return;
            }

            // Shuffle questions for randomization
            questionsToAsk = questionsToAsk.OrderBy(x => _random.Next()).ToList();

            int correctAnswers = 0;
            List<int> wrongAnswerIds = new List<int>();

            for (int i = 0; i < questionsToAsk.Count; i++)
            {
                var flashcard = questionsToAsk[i];
                Console.WriteLine($"Question {i + 1}/{questionsToAsk.Count}:");
                Console.WriteLine($"{flashcard.Question}");
                Console.Write("Your answer: ");

                string userAnswer = Console.ReadLine() ?? string.Empty;

                if (userAnswer.Trim().Equals(flashcard.Answer.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✓ Correct!");
                    correctAnswers++;
                }
                else
                {
                    Console.WriteLine($"✗ Incorrect. The correct answer is: {flashcard.Answer}");
                    wrongAnswerIds.Add(flashcard.Id);
                }

                Console.WriteLine(new string('-', 40));
            }

            // Calculate and display results
            decimal score = questionsToAsk.Count > 0 ? (decimal)correctAnswers / questionsToAsk.Count * 100 : 0;

            Console.WriteLine($"\n=== QUIZ RESULTS ===");
            Console.WriteLine($"Subject: {subjectName}");
            Console.WriteLine($"Questions: {questionsToAsk.Count}");
            Console.WriteLine($"Correct: {correctAnswers}");
            Console.WriteLine($"Wrong: {questionsToAsk.Count - correctAnswers}");
            Console.WriteLine($"Score: {score:F1}%");

            if (score >= 80)
            {
                Console.WriteLine("🎉 Excellent work!");
            }
            else if (score >= 60)
            {
                Console.WriteLine("👍 Good job! Keep studying!");
            }
            else
            {
                Console.WriteLine("📚 Keep practicing - you'll get better!");
            }

            // Save quiz results
            var quizResult = new QuizResult
            {
                SubjectName = subjectName,
                TotalQuestions = questionsToAsk.Count,
                CorrectAnswers = correctAnswers,
                WrongAnswerIds = wrongAnswerIds
            };

            _performanceTracker.AddQuizResult(quizResult);
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    public class PerformanceTracker
    {
        private UserPerformance _userPerformance = new UserPerformance();
        private readonly string _performanceFile = "performance.json";

        public PerformanceTracker()
        {
            LoadPerformance();
        }

        public void LoadPerformance()
        {
            try
            {
                if (File.Exists(_performanceFile))
                {
                    string jsonString = File.ReadAllText(_performanceFile);
                    _userPerformance = JsonSerializer.Deserialize<UserPerformance>(jsonString) ?? new UserPerformance();
                    Console.WriteLine("Performance data loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading performance data: {ex.Message}");
                _userPerformance = new UserPerformance();
            }
        }

        public void SavePerformance()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_userPerformance, options);
                File.WriteAllText(_performanceFile, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving performance data: {ex.Message}");
            }
        }

        public void AddQuizResult(QuizResult result)
        {
            _userPerformance.QuizHistory.Add(result);

            // Update wrong answers dictionary
            if (result.WrongAnswerIds.Count > 0)
            {
                if (!_userPerformance.WrongAnswersBySubject.ContainsKey(result.SubjectName))
                {
                    _userPerformance.WrongAnswersBySubject[result.SubjectName] = new List<int>();
                }

                // Add new wrong answers and remove duplicates
                _userPerformance.WrongAnswersBySubject[result.SubjectName].AddRange(result.WrongAnswerIds);
                _userPerformance.WrongAnswersBySubject[result.SubjectName] =
                    _userPerformance.WrongAnswersBySubject[result.SubjectName].Distinct().ToList();
            }

            SavePerformance();
        }

        public List<int> GetWrongAnswers(string subjectName)
        {
            if (_userPerformance.WrongAnswersBySubject.ContainsKey(subjectName))
            {
                return _userPerformance.WrongAnswersBySubject[subjectName];
            }
            return new List<int>();
        }

        public void ClearWrongAnswers(string subjectName, List<int> correctlyAnsweredIds)
        {
            if (_userPerformance.WrongAnswersBySubject.ContainsKey(subjectName))
            {
                foreach (int id in correctlyAnsweredIds)
                {
                    _userPerformance.WrongAnswersBySubject[subjectName].Remove(id);
                }
                SavePerformance();
            }
        }

        public void DisplayPerformance()
        {
            Console.Clear();
            Console.WriteLine("=== PERFORMANCE SUMMARY ===\n");

            if (_userPerformance.QuizHistory.Count == 0)
            {
                Console.WriteLine("No quiz history available yet.");
                Console.WriteLine("Take some quizzes to see your performance!");
                return;
            }

            // Overall statistics
            var totalQuizzes = _userPerformance.QuizHistory.Count;
            var totalQuestions = _userPerformance.QuizHistory.Sum(q => q.TotalQuestions);
            var totalCorrect = _userPerformance.QuizHistory.Sum(q => q.CorrectAnswers);
            var overallScore = totalQuestions > 0 ? (decimal)totalCorrect / totalQuestions * 100 : 0;

            Console.WriteLine($"Total Quizzes Taken: {totalQuizzes}");
            Console.WriteLine($"Total Questions Answered: {totalQuestions}");
            Console.WriteLine($"Total Correct Answers: {totalCorrect}");
            Console.WriteLine($"Overall Score: {overallScore:F1}%");
            Console.WriteLine(new string('-', 40));

            // Performance by subject
            var subjectGroups = _userPerformance.QuizHistory.GroupBy(q => q.SubjectName);

            Console.WriteLine("\nPERFORMANCE BY SUBJECT:");
            foreach (var group in subjectGroups)
            {
                var subjectQuizzes = group.Count();
                var subjectQuestions = group.Sum(q => q.TotalQuestions);
                var subjectCorrect = group.Sum(q => q.CorrectAnswers);
                var subjectScore = subjectQuestions > 0 ? (decimal)subjectCorrect / subjectQuestions * 100 : 0;
                var lastQuizDate = group.Max(q => q.QuizDate);

                Console.WriteLine($"\n📚 {group.Key}:");
                Console.WriteLine($"   Quizzes: {subjectQuizzes}");
                Console.WriteLine($"   Questions: {subjectQuestions}");
                Console.WriteLine($"   Correct: {subjectCorrect}");
                Console.WriteLine($"   Score: {subjectScore:F1}%");
                Console.WriteLine($"   Last Quiz: {lastQuizDate:yyyy-MM-dd HH:mm}");

                // Show wrong answers count
                var wrongAnswersCount = GetWrongAnswers(group.Key).Count;
                if (wrongAnswersCount > 0)
                {
                    Console.WriteLine($"   📝 Questions to retest: {wrongAnswersCount}");
                }
            }

            // Recent quiz history
            Console.WriteLine("\nRECENT QUIZ HISTORY:");
            var recentQuizzes = _userPerformance.QuizHistory
                .OrderByDescending(q => q.QuizDate)
                .Take(5);

            foreach (var quiz in recentQuizzes)
            {
                Console.WriteLine($"{quiz.QuizDate:yyyy-MM-dd HH:mm} | {quiz.SubjectName} | " +
                                $"{quiz.CorrectAnswers}/{quiz.TotalQuestions} ({quiz.Score:F1}%)");
            }
        }
    }

    // Main Program Class
    public class Program
    {
        private static FlashcardManager _flashcardManager = new FlashcardManager();
        private static PerformanceTracker _performanceTracker = new PerformanceTracker();
        private static QuizEngine _quizEngine = new QuizEngine(_flashcardManager, _performanceTracker);

        public static void Main(string[] args)
        {
            Console.WriteLine("🎓 Welcome to StudyMate - Your Personal Flashcard Quiz System!");
            Console.WriteLine("===============================================================\n");

            bool running = true;
            while (running)
            {
                try
                {
                    DisplayMainMenu();
                    Console.Write("Choose an option (1-5): ");
                    string choice = Console.ReadLine() ?? string.Empty;

                    switch (choice)
                    {
                        case "1":
                            ManageFlashcards();
                            break;
                        case "2":
                            TakeQuiz();
                            break;
                        case "3":
                            _performanceTracker.DisplayPerformance();
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                            break;
                        case "4":
                            RetestWrongAnswers();
                            break;
                        case "5":
                            Console.WriteLine("Thank you for using StudyMate! Happy studying! 📚");
                            running = false;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please choose 1-5.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                if (running)
                {
                    Console.Clear();
                }
            }
        }

        private static void DisplayMainMenu()
        {
            Console.WriteLine("🎓 STUDYMATE - MAIN MENU");
            Console.WriteLine("========================");
            Console.WriteLine("1. 📝 Create & Manage Flashcards");
            Console.WriteLine("2. 🧠 Take a Quiz");
            Console.WriteLine("3. 📊 View Performance");
            Console.WriteLine("4. 🔄 Retest Wrong Answers");
            Console.WriteLine("5. 🚪 Exit");
            Console.WriteLine();
        }

        private static void ManageFlashcards()
        {
            Console.Clear();
            Console.WriteLine("📝 FLASHCARD MANAGEMENT");
            Console.WriteLine("=======================");
            Console.WriteLine("1. Create New Subject");
            Console.WriteLine("2. Add Flashcard to Subject");
            Console.WriteLine("3. View All Subjects");
            Console.WriteLine("4. View Flashcards in Subject");
            Console.WriteLine("5. Delete Flashcard");
            Console.WriteLine("6. Back to Main Menu");
            Console.WriteLine();

            Console.Write("Choose an option (1-6): ");
            string choice = Console.ReadLine() ?? string.Empty;

            switch (choice)
            {
                case "1":
                    CreateNewSubject();
                    break;
                case "2":
                    AddFlashcardToSubject();
                    break;
                case "3":
                    ViewAllSubjects();
                    break;
                case "4":
                    ViewFlashcardsInSubject();
                    break;
                case "5":
                    DeleteFlashcard();
                    break;
                case "6":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please choose 1-6.");
                    break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void CreateNewSubject()
        {
            Console.Write("Enter subject name: ");
            string subjectName = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subjectName))
            {
                Console.WriteLine("Subject name cannot be empty!");
                return;
            }

            _flashcardManager.AddSubject(subjectName.Trim());
        }

        private static void AddFlashcardToSubject()
        {
            var subjects = _flashcardManager.GetAllSubjects();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No subjects available. Create a subject first!");
                return;
            }

            Console.WriteLine("Available subjects:");
            for (int i = 0; i < subjects.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {subjects[i].Name}");
            }

            Console.Write("Select subject number: ");
            if (int.TryParse(Console.ReadLine(), out int subjectIndex) &&
                subjectIndex >= 1 && subjectIndex <= subjects.Count)
            {
                string subjectName = subjects[subjectIndex - 1].Name;

                Console.Write("Enter question: ");
                string question = Console.ReadLine() ?? string.Empty;

                Console.Write("Enter answer: ");
                string answer = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
                {
                    Console.WriteLine("Both question and answer are required!");
                    return;
                }

                _flashcardManager.AddFlashcard(subjectName, question.Trim(), answer.Trim());
            }
            else
            {
                Console.WriteLine("Invalid subject selection!");
            }
        }

        private static void ViewAllSubjects()
        {
            var subjects = _flashcardManager.GetAllSubjects();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No subjects available.");
                return;
            }

            Console.WriteLine("📚 ALL SUBJECTS:");
            Console.WriteLine(new string('-', 30));

            for (int i = 0; i < subjects.Count; i++)
            {
                var subject = subjects[i];
                Console.WriteLine($"{i + 1}. {subject.Name} ({subject.Flashcards.Count} flashcards)");
                Console.WriteLine($"   Created: {subject.CreatedDate:yyyy-MM-dd}");
            }
        }

        private static void ViewFlashcardsInSubject()
        {
            var subjects = _flashcardManager.GetAllSubjects();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No subjects available.");
                return;
            }

            Console.WriteLine("Select subject to view flashcards:");
            for (int i = 0; i < subjects.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {subjects[i].Name}");
            }

            Console.Write("Select subject number: ");
            if (int.TryParse(Console.ReadLine(), out int subjectIndex) &&
                subjectIndex >= 1 && subjectIndex <= subjects.Count)
            {
                var subject = subjects[subjectIndex - 1];

                if (subject.Flashcards.Count == 0)
                {
                    Console.WriteLine($"No flashcards in '{subject.Name}' yet.");
                    return;
                }

                Console.WriteLine($"\n🃏 FLASHCARDS IN '{subject.Name}':");
                Console.WriteLine(new string('-', 40));

                foreach (var flashcard in subject.Flashcards)
                {
                    Console.WriteLine($"ID: {flashcard.Id}");
                    Console.WriteLine($"Q: {flashcard.Question}");
                    Console.WriteLine($"A: {flashcard.Answer}");
                    Console.WriteLine($"Created: {flashcard.CreatedDate:yyyy-MM-dd}");
                    Console.WriteLine(new string('-', 40));
                }
            }
            else
            {
                Console.WriteLine("Invalid subject selection!");
            }
        }

        private static void DeleteFlashcard()
        {
            var subjects = _flashcardManager.GetAllSubjects();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No subjects available.");
                return;
            }

            Console.WriteLine("Select subject:");
            for (int i = 0; i < subjects.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {subjects[i].Name}");
            }

            Console.Write("Select subject number: ");
            if (int.TryParse(Console.ReadLine(), out int subjectIndex) &&
                subjectIndex >= 1 && subjectIndex <= subjects.Count)
            {
                var subject = subjects[subjectIndex - 1];

                if (subject.Flashcards.Count == 0)
                {
                    Console.WriteLine($"No flashcards in '{subject.Name}' to delete.");
                    return;
                }

                Console.WriteLine($"\nFlashcards in '{subject.Name}':");
                foreach (var flashcard in subject.Flashcards)
                {
                    Console.WriteLine($"ID: {flashcard.Id} - Q: {flashcard.Question}");
                }

                Console.Write("Enter flashcard ID to delete: ");
                if (int.TryParse(Console.ReadLine(), out int flashcardId))
                {
                    _flashcardManager.DeleteFlashcard(subject.Name, flashcardId);
                }
                else
                {
                    Console.WriteLine("Invalid flashcard ID!");
                }
            }
            else
            {
                Console.WriteLine("Invalid subject selection!");
            }
        }

        private static void TakeQuiz()
        {
            var subjects = _flashcardManager.GetAllSubjects().Where(s => s.Flashcards.Count > 0).ToList();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No subjects with flashcards available. Create some flashcards first!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine("🧠 QUIZ MODE");
            Console.WriteLine("=============");
            Console.WriteLine("Select a subject to quiz:");

            for (int i = 0; i < subjects.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {subjects[i].Name} ({subjects[i].Flashcards.Count} flashcards)");
            }

            Console.Write("Select subject number: ");
            if (int.TryParse(Console.ReadLine(), out int subjectIndex) &&
                subjectIndex >= 1 && subjectIndex <= subjects.Count)
            {
                string subjectName = subjects[subjectIndex - 1].Name;
                _quizEngine.StartQuiz(subjectName);
            }
            else
            {
                Console.WriteLine("Invalid subject selection!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static void RetestWrongAnswers()
        {
            var subjects = _flashcardManager.GetAllSubjects();
            var subjectsWithWrongAnswers = subjects.Where(s =>
                _performanceTracker.GetWrongAnswers(s.Name).Count > 0).ToList();

            if (subjectsWithWrongAnswers.Count == 0)
            {
                Console.WriteLine("No wrong answers to retest! Take some quizzes first.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine("🔄 RETEST WRONG ANSWERS");
            Console.WriteLine("========================");
            Console.WriteLine("Select a subject to retest:");

            for (int i = 0; i < subjectsWithWrongAnswers.Count; i++)
            {
                var subject = subjectsWithWrongAnswers[i];
                int wrongCount = _performanceTracker.GetWrongAnswers(subject.Name).Count;
                Console.WriteLine($"{i + 1}. {subject.Name} ({wrongCount} questions to retest)");
            }

            Console.Write("Select subject number: ");
            if (int.TryParse(Console.ReadLine(), out int subjectIndex) &&
                subjectIndex >= 1 && subjectIndex <= subjectsWithWrongAnswers.Count)
            {
                string subjectName = subjectsWithWrongAnswers[subjectIndex - 1].Name;
                _quizEngine.StartQuiz(subjectName, isRetestMode: true);
            }
            else
            {
                Console.WriteLine("Invalid subject selection!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}

