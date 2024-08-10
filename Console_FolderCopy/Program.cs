using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_FolderCopy
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.IO;

    class Program
    {
        static void Main()
        {
            //Directory.Move("D:\\PHB\\Docs1", "D:\\PHB\\Docs2");
            string connectionString = ConfigurationManager.AppSettings["dbcon"];

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //string query = "SELECT ID, SourceFolder, TargetFolder, Action FROM FolderConfiguration WHERE Status = 'New'";
                string query = "p_get_file_name";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = (int)reader["FolderId"];
                        string sourceFolder = reader["TFolderName"].ToString();
                        string targetFolder = reader["target_folder"].ToString();
                        string action = reader["OperationType"].ToString();

                        Console.WriteLine($"Copying folder of Id : {id}");

                        try
                        {
                            if (!Directory.Exists(sourceFolder))
                            {
                                Directory.CreateDirectory(targetFolder);
                                Console.WriteLine($"Source folder does not exist: {sourceFolder}");
                                UpdateStatus(connection, id, "SourceNotFound");
                                continue;
                            }

                            if (action.Equals("Copy", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!Directory.Exists(targetFolder))
                                {
                                    Directory.CreateDirectory(targetFolder);
                                }

                                CopyDirectory(sourceFolder, targetFolder);
                            }
                            else if (action.Equals("Move", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Directory.Exists(targetFolder))
                                {
                                    Directory.Delete(targetFolder, true);
                                }

                                MoveDirectory(sourceFolder, targetFolder);
                            }
                            else
                            {
                                Console.WriteLine($"Unknown action: {action} for source folder: {sourceFolder}");
                                UpdateStatus(connection, id, "UnknownAction");
                                continue;
                            }

                            UpdateStatus(connection, id, "Done");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing {sourceFolder} to {targetFolder}: {ex.Message}");
                        }
                    }
                }
            }
        }

        static void CopyDirectory(string sourceDir, string targetDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
        }

        static void MoveDirectory(string sourceDir, string targetDir)
        {
            Directory.Move(sourceDir, targetDir);
            //foreach (string directory in Directory.GetDirectories(sourceDir))
            //{
            //    string destDir = Path.Combine(targetDir, Path.GetFileName(directory));
            //    MoveDirectory(directory, destDir);
            //}

            //foreach (string file in Directory.GetFiles(sourceDir))
            //{
            //    string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            //    File.Move(file, destFile);
            //}

            //Directory.Delete(sourceDir, true);
        }

        static void UpdateStatus(SqlConnection connection, int id, string status)
        {
            string updateQuery = "p_update_file_status";

            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
            {
                updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                updateCommand.Parameters.AddWithValue("@Status", status);
                updateCommand.Parameters.AddWithValue("@ID", id);

                updateCommand.ExecuteNonQuery();
            }
        }
    }
}