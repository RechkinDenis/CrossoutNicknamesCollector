using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CrossoutNicknamesCollector
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sqlConnect;

        public string saveNickNameCommand = "./save";
        public string pathToLogsFile = "D:\\Logs\\";
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
        public string playersFileDB = "analytics.db";

        public HashSet<string> analiticsPlayer = new HashSet<string>();
        public HashSet<string> analiticsPlayerChat = new HashSet<string>();

        public List<string> allSortPlayers => SortNickNames().ToList();

        public string LocalDerictory => AppDomain.CurrentDomain.BaseDirectory;

        public string DuplicateLogsDerictory => $"{LocalDerictory}\\{duplicateFolderLogs}";

        public string PathToPlayersDB => $"{LocalDerictory}//{playersFileDB}";

        public string[] NickNames => GetNicknamesFromDatabase(ConnectionStringPlayers, "Players");
        
        public int CountPlayers => Convert.ToInt32($"{CountRowsInDatabase($"Data Source={playersFileDB};Version=3;", "Players")}");

        public int LastCountPlayers => CheckLastCountPlayers($"{LocalDerictory}\\{lastCountPlayers}");

        public Form1()
        {
            InitializeComponent();

            if (!File.Exists(PathToPlayersDB)) 
            { 
                label4.Text = "file DB is missing";
                button2.Enabled = false;

                // TODO CREATE DB
            }
            else 
            { 
                label4.Text = "file DB exists"; 
                button2.Enabled = true;

                // Open sql connection
                sqlConnect = new SQLiteConnection($"Data Source={PathToPlayersDB};Version=3;");
                sqlConnect.Open();
            }
        }

        public string[] GetNicknamesFromDatabase(string connectionString, string tableName)
        {
            List<string> nicknamesList = new List<string>();

            SQLiteConnection connection = sqlConnect;
            
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

        public string GetNicknameWithoutID(string input = "null")
        {
            string pattern = @"\s*#\d+\s*";
            string nickname = Regex.Replace(input, pattern, "");
            return nickname.TrimEnd();
        }

        public void CheckAndCreateTable(string dbPath)
        {
            try
            {
                var connection = sqlConnect;
                
                    // Проверяем наличие таблицы "Players" в базе данных
                    string checkTableQuery = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Players'";
                    using (var command = new SQLiteCommand(checkTableQuery, connection))
                    {
                        if (!TableExists(PathToPlayersDB, "Players"))
                        {
                            // Если таблицы "Players" нет, то создаем её без автоинкремента в столбце id
                            string createTableQuery = "CREATE TABLE IF NOT EXISTS Players (nickname TEXT)";
                            using (var createCommand = new SQLiteCommand(createTableQuery, connection))
                            {
                                createCommand.ExecuteNonQuery();
                                Console.WriteLine("Таблица 'Players' успешно создана.");
                            }
                        }
                    }

                    // Проверяем наличие столбца "nickname" в таблице
                    string checkColumnQuery = "PRAGMA table_info('Players')";
                    using (var columnCommand = new SQLiteCommand(checkColumnQuery, connection))
                    {
                        using (var reader = columnCommand.ExecuteReader())
                        {
                            bool columnExists = false;
                            while (reader.Read())
                            {
                                string columnName = reader["name"].ToString();
                                if (columnName == "nickname")
                                {
                                    columnExists = true;
                                    break;
                                }
                            }

                            if (!columnExists)
                            {
                                // Если столбца "nickname" нет, то добавляем его
                                string addColumnQuery = "ALTER TABLE Players ADD COLUMN nickname TEXT";
                                using (var addColumnCommand = new SQLiteCommand(addColumnQuery, connection))
                                {
                                    addColumnCommand.ExecuteNonQuery();
                                    Console.WriteLine("Столбец 'nickname' успешно добавлен в таблицу 'Players'.");
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

        public bool TableExists(string dbPath, string tableName)
        {
            try
            {
                var connection = sqlConnect;
                
                    string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                    using (var command = new SQLiteCommand(checkTableQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                return false;
            }
        }

        private void CreateDB(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    File.Create(path);
                    //CreateDatabase(path);
                }
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }
            CheckAndCreateTable(path);
        }

        public void CreateDatabase(string dbPath)
        {
            try
            {
                // Подключение к базе данных
                var connection = sqlConnect;
                
                    // Создание таблицы Players с одним столбцом nickname
                    using (var command = new SQLiteCommand($"CREATE TABLE IF NOT EXISTS Players (nickname TEXT)", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Таблица Players с одним столбцом nickname создана.");
                    }
                
            }
            catch (Exception ex)
            {
                label1.Text = "Ошибка при создании базы данных: " + ex.Message;
            }
        }

        public void CheckAndAddNickname(string dbPath, string nickname)
        {
            try
            {
                var connection = sqlConnect;
                
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
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        public void ClearDatabase(string dbPath)
        {
            try
            {
                var connection = sqlConnect;
                
                    // Выполняем SQL-запрос для удаления всех данных из таблицы
                    string query = "DELETE FROM Players";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine("База данных успешно очищена.");
                
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
                var connection = sqlConnect;
                
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

            SQLiteConnection connection = sqlConnect;
            
                using (SQLiteCommand command = new SQLiteCommand($"SELECT COUNT(*) FROM {from}", connection))
                {
                    rowCount = Convert.ToInt32(command.ExecuteScalar());
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

        static List<string> GetUniqueNicknames(List<string> currentNicknames, List<string> newNicknames)
        {
            // Используем LINQ для получения уникальных элементов из второго списка (newNicknames),
            // которых нет в первом списке (currentNicknames)
            List<string> uniqueNicknames = newNicknames.Except(currentNicknames).ToList();
            return uniqueNicknames;
        }

        List<string> AllPlayers(HashSet<string> chat, HashSet<string> game)
        {
            List<string> nicknames = new List<string>();

            nicknames.AddRange(chat);
            nicknames.AddRange(game);

            return nicknames;
        }

        public void NewBatchInsertNicknames(string dbPath, List<string> nicknames)
        {
            List<string> nicknamesToAdd = new List<string>();

            nicknamesToAdd = GetUniqueNicknames(NickNames.ToList(), AllPlayers(ReadPlayerNicknamesFromLogsChat(DuplicateLogsDerictory), ReadPlayerNicknamesFromLogsGame(DuplicateLogsDerictory)));

            int batchSize = 100;

            SQLiteConnection connection = sqlConnect;
            
                try
                {
                    // Создаем SQL-запрос на добавление записи
                    string insertQuery = "INSERT INTO Players (nickname) VALUES (@Nickname)";

                    // Создаем команду с параметром для безопасного добавления значения nickname
                    using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                    {
                        // Добавляем параметр к команде
                        SQLiteParameter parameter = command.Parameters.AddWithValue("@Nickname", null);

                        int totalNicknames = nicknamesToAdd.Count;
                        int batchesCount = (int)Math.Ceiling((double)totalNicknames / batchSize);

                        // Используем цикл для вставки каждой части никнеймов
                        for (int i = 0; i < batchesCount; i++)
                        {
                            int startIndex = i * batchSize;
                            int count = Math.Min(batchSize, totalNicknames - startIndex);

                            // Создаем пакет значений для вставки
                            List<string> batchValues = nicknamesToAdd.GetRange(startIndex, count);

                            // Очищаем параметры команды
                            parameter.Value = null;

                            // Вставляем пакет значений
                            foreach (string nickname in batchValues)
                            {
                                parameter.Value = nickname;
                                command.ExecuteNonQuery();
                            }

                            Console.WriteLine($"Добавлено {count} никнеймов. Всего добавлено: {startIndex + count}");
                        }

                        Console.WriteLine("Вставка завершена.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при добавлении записей: " + ex.Message);
                }
            
        }
    
        /*
        public void BatchInsertNicknames(string dbPath, List<string> nicknames)
        {
            try
            {
                
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // Определяем размер пакета
                    int batchSize = 100;

                    for (int i = 0; i < sortNickNames.Count; i += batchSize)
                    {
                        // Получаем текущий пакет никнеймов
                        List<string> batchNicknames = sortNickNames.Skip(i).Take(batchSize).ToList();

                        // Формируем SQL-запрос для пакетной вставки
                        StringBuilder queryBuilder = new StringBuilder();
                        queryBuilder.Append("INSERT INTO Players (nickname) VALUES ");

                        for (int j = 0; j < batchNicknames.Count; j++)
                        {
                            queryBuilder.Append($"(@nickname{j})");
                        }

                        // Выполняем пакетную вставку
                        using (var command = new SQLiteCommand(queryBuilder.ToString(), connection))
                        {
                            for (int j = 0; j < batchNicknames.Count; j++)
                            {
                                command.Parameters.AddWithValue($"@nickname{j}", batchNicknames[j]);
                            }

                            command.ExecuteNonQuery();
                        }
                    }

                    Console.WriteLine("Пакетная вставка никнеймов успешно завершена.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }
        */
        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Analitics
            DeleteFolderRecursively(DuplicateLogsDerictory);
            DuplicateFolders(pathToLogsFile, DuplicateLogsDerictory);

            AllPlayers(ReadPlayerNicknamesFromLogsChat(DuplicateLogsDerictory), ReadPlayerNicknamesFromLogsGame(DuplicateLogsDerictory));

            CreateDB(PathToPlayersDB);

            //старт


            Stopwatch watch = new Stopwatch();

            watch.Start();

            //foreach (string player in SortNickNames())
            //{
            //    CheckAndAddNickname($"{LocalDerictory}\\{playersFileDB}", player); //old
            //}

            NewBatchInsertNicknames(PathToPlayersDB, allSortPlayers);//new

            watch.Stop();

            label1.Text = $"recording ended Milliseconds: {watch.ElapsedMilliseconds}";

            //label1.Text = "recording ended";
            //конец конца

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

        private void button6_Click(object sender, EventArgs e)
        {
            //Create DB

            CreateDB(PathToPlayersDB);
            if (!File.Exists(PathToPlayersDB))
            {
                label4.Text = "file DB is missing";
            }
            else
            {
                label4.Text = "file DB exists or has been created";
                button2.Enabled = true;
            }
        }
    }
}