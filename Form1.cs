using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EnemyDB
{
    public partial class Form1 : Form
    {
        public string saveNickNameCommand = "./save";
        public string pathToLogsFile = "C:\\Users\\denk1\\OneDrive\\Документы\\My Games\\Crossout\\logs\\";
        public string chatLog = "chat.log";
        public string gameLog = "game.log";
        public string lastCountPlayers = "lastUpdate.txt";
        public string NicnamesTxt = "Nicknames.txt";
        public string privateChat = "<    PRIVATE To  >";
        public string generalChat = "<   general_pc_ru>";

        //
        public string gameMaster = "GM";
        public string chatHelp = "ChatHelp";

        //Duplicate
        public string duplicateFileLog = "duplicateChat.log";
        public string duplicateFolderLogs = "Logs";

        //DB
        public string ConnectionStringPlayers => $"Data Source={playersFileDB};Version=3;";
        public string enemyFileDB = "enemy.db";
        public string playersFileDB = "analytics.db";

        HashSet<string> analiticsPlayer = new HashSet<string>();
        HashSet<string> analiticsPlayerChat = new HashSet<string>();

        public string LocalDerictory => AppDomain.CurrentDomain.BaseDirectory;
        public string DuplicateLogsDerictory => $"{LocalDerictory}\\{duplicateFolderLogs}";

        public string PathToPlayersDB => $"{LocalDerictory}//{playersFileDB}";
        public string PathToEnemyDB => $"{LocalDerictory}//{enemyFileDB}";

        public string[] NickNames => GetNicknamesFromDatabase(ConnectionStringPlayers, "Players");
        
        public int CountPlayers => Convert.ToInt32($"{CountRowsInDatabase($"Data Source={playersFileDB};Version=3;", "Players")}");
        public int LastCountPlayers => CheckLastCountPlayers($"{LocalDerictory}\\{lastCountPlayers}");

        public Form1()
        {
            InitializeComponent();
        }

        public static string[] GetNicknamesFromDatabase(string connectionString, string tableName)
        {
            List<string> nicknamesList = new List<string>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string sqlQuery = $"SELECT nickname FROM {tableName}";

                using (SQLiteCommand command = new SQLiteCommand(sqlQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string nickname = reader.GetString(0);
                            nicknamesList.Add(nickname);
                        }
                    }
                }
            }

            return nicknamesList.ToArray();
        }

        public void DeleteFolderRecursively(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                // Удаляем все файлы в папке
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                // Рекурсивно удаляем все подпапки
                string[] subdirectories = Directory.GetDirectories(folderPath);
                foreach (string subdirectory in subdirectories)
                {
                    DeleteFolderRecursively(subdirectory);
                }

                // Удаляем саму папку
                Directory.Delete(folderPath);
            }
        }

        public void DuplicateFolders(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (string subdirectory in Directory.GetDirectories(sourcePath))
            {
                string subdirectoryName = Path.GetFileName(subdirectory);
                string destinationSubdirectory = Path.Combine(destinationPath, subdirectoryName);

                DuplicateFolders(subdirectory, destinationSubdirectory);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(file);
                string destinationFile = Path.Combine(destinationPath, fileName);

                File.Copy(file, destinationFile);
            }
        }

        public string GetNewestFolder(string folderPath)
        {
            try
            {
                var folderDirectories = Directory.GetDirectories(folderPath);

                var sortedFolders = folderDirectories.OrderByDescending(d => Directory.GetLastWriteTime(d)).ToList();

                if (sortedFolders.Any())
                {
                    return sortedFolders.First();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            return null;
        }

        public static string FindLastNicknameInPrivateMessage(string logFilePath, string command)
        {
            string nickname = null;

            try
            {
                string[] lines = File.ReadAllLines(logFilePath);

                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i];
                    if (line.Contains(command) && line.Contains("<    PRIVATE To  >"))
                    {
                        // Используем регулярное выражение для извлечения никнейма из квадратных скобок
                        Match match = Regex.Match(line, @"\[\s*(.*?)\s*]");
                        if (match.Success)
                        {
                            nickname = match.Groups[1].Value;
                            // Не прерываем цикл, чтобы найти последнее сообщение с командой
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            return nickname;
        }

        public string GetNicknameWithoutID(string input = "null")
        {
            string pattern = @"\s*#\d+\s*";
            string nickname = Regex.Replace(input, pattern, "");
            return nickname.TrimEnd();
        }

        private void CreateDB(string path, string name)
        {
            string pathToDB = path + name;
            if (!File.Exists(pathToDB))
            {
                File.Create(pathToDB);
                CreateDatabase(pathToDB);
                CreateDatabase(pathToDB);
            }
        }

        public void CreateDatabase(string dbPath)
        {
            try
            {
                // Подключение к базе данных
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Создание таблицы Users с одним столбцом nickname
                    using (var command = new SQLiteCommand($"CREATE TABLE IF NOT EXISTS Players (nickname TEXT)", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Таблица Users с одним столбцом nickname создана.");
                    }
                }
            }
            catch (Exception ex)
            {
                label1.Text = "Ошибка при создании базы данных: " + ex.Message;
            }
        }

        public static void CheckAndAddNickname(string dbPath, string nickname)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Начинаем транзакцию
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Проверяем, существует ли никнейм в базе данных
                        using (var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM Players WHERE nickname = @nickname", connection))
                        {
                            checkCommand.Parameters.AddWithValue("@nickname", nickname);
                            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                            if (count > 0)
                            {
                                Console.WriteLine("Никнейм уже существует в базе данных.");
                            }
                            else
                            {
                                // Добавляем никнейм в базу данных
                                using (var insertCommand = new SQLiteCommand("INSERT INTO Players (nickname) VALUES (@nickname)", connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@nickname", nickname);
                                    insertCommand.ExecuteNonQuery();
                                    Console.WriteLine("Никнейм успешно добавлен в базу данных.");
                                }
                            }
                        }

                        // Завершаем транзакцию
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        public static void ClearDatabase(string dbPath)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Выполняем SQL-запрос для удаления всех данных из таблицы
                    string query = "DELETE FROM Players";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine("База данных успешно очищена.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        public static void ReadAllNicknames(string dbPath, List<string> nicknamesList)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Выбираем все записи из таблицы Users
                    using (var command = new SQLiteCommand("SELECT nickname FROM Users", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string nickname = reader["nickname"].ToString();
                                nicknamesList.Add(nickname); // Добавляем никнейм в список
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        public void DeleteNickname(string dbPath, string nicknameToDelete, string table)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Выполняем SQL-запрос DELETE для удаления никнейма
                    using (var command = new SQLiteCommand($"DELETE FROM {table} WHERE nickname = @nickname", connection))
                    {
                        command.Parameters.AddWithValue("@nickname", nicknameToDelete);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            label1.Text = $"Никнейм '{nicknameToDelete}' успешно удален из базы данных.";
                        }
                        else
                        {
                            label1.Text = $"Никнейм '{nicknameToDelete}' не найден в базе данных.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                label1.Text = ex.Message;
            }
        }

        public HashSet<string> ReadPlayerNicknamesFromLogsChat(string logsFolderPath)
        {
            HashSet<string> playerNicknames = new HashSet<string>();

            try
            {
                DirectoryInfo logsDirectory = new DirectoryInfo(logsFolderPath);
                FileInfo[] logFiles = logsDirectory.GetFiles(chatLog, SearchOption.AllDirectories);

                foreach (FileInfo logFile in logFiles)
                {
                    string[] lines = File.ReadAllLines(logFile.FullName);
                    foreach (string line in lines)
                    {
                        
                        string nickname = null;
                        Match match = Regex.Match(line, @"\[\s*(.*?)\s*]");
                        if (match.Success)
                        {
                            nickname = GetNicknameWithoutID(match.Groups[1].Value);
                            playerNicknames.Add(nickname);
                            // Не прерываем цикл, чтобы найти последнее сообщение с командой
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }

            return playerNicknames;
        }

        public HashSet<string> ReadPlayerNicknamesFromLogsGame(string logsFolderPath)
        {
            HashSet<string> playerNicknames = new HashSet<string>();

            try
            {
                DirectoryInfo logsDirectory = new DirectoryInfo(logsFolderPath);
                FileInfo[] logFiles = logsDirectory.GetFiles(gameLog, SearchOption.AllDirectories);

                foreach (FileInfo logFile in logFiles)
                {
                    string[] lines = File.ReadAllLines(logFile.FullName);
                    foreach (string line in lines)
                    {
                        // Используем регулярное выражение для поиска строки с ADD_PLAYER и никнеймом
                        Match match = Regex.Match(line, @"client: ADD_PLAYER\s+\d+\s+(.*?),");

                        if (match.Success)
                        {
                            string nickname = match.Groups[1].Value.Trim();
                            playerNicknames.Add(nickname);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            return playerNicknames;
        }

        public int CountRowsInDatabase(string connectionString, string from)
        {
            int rowCount = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"SELECT COUNT(*) FROM {from}", connection))
                {
                    rowCount = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return rowCount;
        }

        public void CreateFileLastCountPlayers(string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                File.WriteAllText(path, "0");
            }
        }

        public int CheckLastCountPlayers(string path)
        {
            int count = 0;
            count = Convert.ToInt32(File.ReadAllText(path));
            return count;
        }

        public void SetLastCountPlayers(string path, string value)
        {
            File.WriteAllText(path, value);
        }

        public int subtractLastCountPlayers(int last, int now)
        {
            return now - last;
        }

        private void CreateNicknamesToTxt(string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
        }

        private void NicknamesToTxt(string path, string[] content)
        {
            if (File.Exists(path))
            {
                // Объединяем строки массива content в одну строку с разделителем "\n"
                string contentString = string.Join("\n", content);
                File.WriteAllText(path, contentString);
            }
        }

        public static List<string> SortNicknames(List<string> nicknamesHashSet)
        {
            // Создаем объект культуры для правильной сортировки
            CultureInfo culture = new CultureInfo("en-US", false);

            // Преобразуем HashSet в List
            List<string> nicknamesList = nicknamesHashSet.ToList();

            // Сортируем никнеймы с использованием культуры
            nicknamesList.Sort(new CustomComparer(culture));

            return nicknamesList;
        }

        // Класс для сравнения строк с использованием заданной культуры
        public class CustomComparer : IComparer<string>
        {
            private readonly CompareInfo compareInfo;

            public CustomComparer(CultureInfo culture)
            {
                compareInfo = culture.CompareInfo;
            }

            public int Compare(string x, string y)
            {
                // Используем Compare метод для сравнения строк с использованием заданной культуры
                return compareInfo.Compare(x, y);
            }
        }

        public string[] SortNickNames()
        {
            HashSet<string> uniqueNicknames = new HashSet<string>();

            foreach (string nickname in analiticsPlayerChat)
            {
                uniqueNicknames.Add(nickname);
            }

            foreach (string nickname in analiticsPlayer)
            {
                uniqueNicknames.Add(nickname);
            }

            List<string> sortedNicknames = SortNicknames(uniqueNicknames.ToList());

            return sortedNicknames.ToArray();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Analitics

            DeleteFolderRecursively(DuplicateLogsDerictory);
            DuplicateFolders(pathToLogsFile, DuplicateLogsDerictory);

            analiticsPlayerChat = ReadPlayerNicknamesFromLogsChat(DuplicateLogsDerictory);
            analiticsPlayer = ReadPlayerNicknamesFromLogsGame(DuplicateLogsDerictory);

            CreateDB($"{LocalDerictory}\\", $"{playersFileDB}");

            foreach (string player in SortNickNames())
            {
                CheckAndAddNickname($"{LocalDerictory}\\{playersFileDB}", player);
            }

            label2.Text = $"Players Count: {CountPlayers}";

            string path = $"{LocalDerictory}\\{lastCountPlayers}";
            CreateFileLastCountPlayers(path);
            label3.Text = $"Last Count: {CheckLastCountPlayers(path)} / + {subtractLastCountPlayers(LastCountPlayers, CountPlayers)}";
            SetLastCountPlayers(path,CountRowsInDatabase($"Data Source={playersFileDB};Version=3;", "Players").ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //To TXT
            string path = $"{LocalDerictory}\\{NicnamesTxt}";
            if (File.Exists(path)) File.Delete(path);
            CreateNicknamesToTxt(path);
            NicknamesToTxt(path, SortNickNames());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Delete Player
            string selectedNickname = (string)listBox1.SelectedItem;
            DeleteNickname(PathToPlayersDB, selectedNickname, "Players");
            listBox1.Items.Remove(selectedNickname);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Update List
            List<string> nicknames = new List<string>();

            nicknames.AddRange(analiticsPlayerChat);
            nicknames.AddRange(analiticsPlayer);

            List<string> sortedNicknames = SortNicknames(nicknames);

            listBox1.Items.Clear();

            foreach(string nickname in sortedNicknames)
            {
                if (!listBox1.Items.Contains(nickname))
                {
                    listBox1.Items.Add(nickname);
                }
            }
        }

    }
}