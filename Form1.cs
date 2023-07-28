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
using System.Threading;
using System.Data;
using System.Data.SqlClient;

namespace CrossoutNicknamesCollector
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sqlConnect;

        public string saveNickNameCommand = "./save";
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
        public string templateFileDB = "template.db";

        public HashSet<string> analiticsPlayer = new HashSet<string>();
        public HashSet<string> analiticsPlayerChat = new HashSet<string>();

        public List<string> allSortPlayers => SortNickNames().ToList();

        public string LocalDerictory => AppDomain.CurrentDomain.BaseDirectory;

        public string DuplicateLogsDerictory => $"{LocalDerictory}\\{duplicateFolderLogs}";

        public string PathToPlayersDB => $"{LocalDerictory}\\{playersFileDB}";

        public string PathToTemplateDB => $"{LocalDerictory}\\{templateFileDB}";

        public string pathToLogsFile => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\Crossout\logs\";

        public string[] NickNames => GetNicknamesFromDatabase(ConnectionStringPlayers, "Players");
        
        public int CountPlayers => Convert.ToInt32($"{CountRowsInDatabase($"Data Source={playersFileDB};Version=3;", "Players")}");

        public int LastCountPlayers => CheckLastCountPlayers($"{LocalDerictory}\\{lastCountPlayers}");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(PathToPlayersDB))
            {
                CreateDB(PathToPlayersDB);
            }

            sqlConnect = new SQLiteConnection($"Data Source={PathToPlayersDB};Version=3;");
            sqlConnect.Open();
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

        private void CreateDB(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    File.Copy(PathToTemplateDB, path);
                }
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
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
            List<string> nicknamesToAdd = GetUniqueNicknames(NickNames.ToList(), AllPlayers(ReadPlayerNicknamesFromLogsChat(DuplicateLogsDerictory), ReadPlayerNicknamesFromLogsGame(DuplicateLogsDerictory)));
            
            List<List<string>> nicknamesLists = new List<List<string>>();
            int listSize = nicknamesToAdd.Count / 8; // TODO: replace 8 by real threads count

            Console.WriteLine("Generating nicknames lists...");
            List<string> tmpNickanames = new List<string>();
            int index = 0;
            foreach(string nickname in nicknamesToAdd)
            {
                if(index >= listSize)
                { // Add batch of nicknames
                    nicknamesLists.Add(tmpNickanames);
                    tmpNickanames = new List<string>();

                    Console.WriteLine($"Generated {nicknamesLists.Count * listSize}...");

                    index = 0;
                }

                // Add nickname to list
                tmpNickanames.Add(nickname);
                index += 1;
            }

            if (index != 0)
            { // Add other nicknames
                nicknamesLists.Add(tmpNickanames);

                Console.WriteLine($"Generated {tmpNickanames.Count}...");
            }

            SQLiteConnection connection = sqlConnect;

            List<Thread> threads = new List<Thread>();

            Console.WriteLine("Starting threads...");
            int threadIndex = 0;
            foreach(List<string> nicknamesList in nicknamesLists)
            {
                int currentThreadIndex = threadIndex;
                Thread t = new Thread(() =>
                {
                    try
                    {
                        string insertQuery = "INSERT INTO Players (nickname) VALUES (@Nickname)";
                        SQLiteTransaction transaction = connection.BeginTransaction();
                        using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                        {
                            command.Transaction = transaction;
                            SQLiteParameter parameter = command.Parameters.AddWithValue("@Nickname", null);

                            foreach (string nickname in nicknamesList)
                            {
                                parameter.Value = nickname;
                                command.ExecuteNonQuery();
                            }
                        }

                        // Commit changes
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при добавлении записей: " + ex.Message);
                    }

                    Console.WriteLine($"Thread finished #{threadIndex}");
                });

                t.Name = $"Thread #{currentThreadIndex}";
                t.IsBackground = true;
                t.Start();

                Console.WriteLine($"Thread started #{currentThreadIndex}");

                threads.Add(t);
                threadIndex++;
            }

            // Wait all threads finish
            while (true)
            {
                Thread.Sleep(100);

                int finishedCount = 0;
                foreach(Thread thread in threads)
                {
                    if (!thread.IsAlive)
                        finishedCount++;
                }

                if (finishedCount == threads.Count)
                    break;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            sqlConnect = new SQLiteConnection($"Data Source={PathToPlayersDB};Version=3;");
            sqlConnect.Open();

            //Analitics
            DeleteFolderRecursively(DuplicateLogsDerictory);
            DuplicateFolders(pathToLogsFile, DuplicateLogsDerictory);

            AllPlayers(ReadPlayerNicknamesFromLogsChat(DuplicateLogsDerictory), ReadPlayerNicknamesFromLogsGame(DuplicateLogsDerictory));

            //старт

            Stopwatch watch = new Stopwatch();
            watch.Start();

            NewBatchInsertNicknames(PathToPlayersDB, allSortPlayers);//new

            watch.Stop();

            label1.Text = $"recording ended Milliseconds: {watch.ElapsedMilliseconds}";
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
    }
}