using Codici;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace classi
{
    class TaskMethods : DataBaseFunctions
    {

        internal async Task<List<TaskClass>> GetVerifiedTaskMethodAsync(string username, string verifica)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------GetVerified/NotVerifiedTasks " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE username='{1}' AND verifica={2} ORDER BY data DESC", taskTable, username, verifica), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaTasks(reader);
            }
            catch
            {
                return null;
            }
        }

        internal async Task<List<TaskClass>> GetEveryTaskMethodAsync(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------GetEveryTask " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE username='{1}'", taskTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaTasks(reader);
            }
            catch
            {
                return null;
            }
        }


        internal async Task<Codes> UpdateVerTasksMethodAsync(string username, string taskName,
                                                       string taskCategory, string taskDate)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------UpdateTask " + username + "----------------------");

                if (!VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, conn).Result)
                    return Codes.TaskDoesNotExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET verifica='true' WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND data='{4}'",
                    taskTable, username, taskCategory, taskName, taskDate), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                int contatore = VerificaEsistenzaTaskMedagliere(username, taskCategory, conn).Result;

                //string Evento = "L'utente " + username + " ha completato il seguente compito: " + taskName;
                //await CreateLogAsync(username, Evento, conn);

                if (contatore == 0)
                    return CreateMedalTaskAsync(username, taskCategory, conn).Result;
                else
                {
                    await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET quantita = {1} WHERE username='{2}' AND nome='{3}'",
                    medalTable, contatore + 1, username, taskCategory), conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.TaskAddError;
            }
        }

        internal async Task<Codes> RemoveTasksMethodAsync(string username, string taskName,
                                                                string taskCategory, string taskDate)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------RemoveTask " + username + "----------------------");

                if (!VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, conn).Result)
                    return Codes.TaskDoesNotExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE FROM {0}  WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND data='{4}'",
                    taskTable, username, taskCategory, taskName, taskDate), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.TaskRemoveError;
            }
        }

        internal async Task<Codes> AddTasksMethodAsync(string username, string taskName,
                                                               string taskCategory, string taskDate,
                                                               string taskDescription, string taskDone)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------AddTask " + username + "----------------------");
                taskDescription = taskDescription.Replace("'", " ");
                taskName = taskName.Replace("'", " ");

                if (VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, conn).Result)
                    return Codes.TaskExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@u, @c, @n, @t, @d, @v)", taskTable), conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("c", taskCategory);
                    cmd.Parameters.AddWithValue("n", taskName);
                    cmd.Parameters.AddWithValue("t", DateTime.Parse(taskDate));
                    cmd.Parameters.AddWithValue("d", taskDescription);
                    cmd.Parameters.AddWithValue("v", Boolean.Parse(taskDone));
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.TaskAddError;
            }
        }


        private async Task<List<TaskClass>> CreazioneListaTasks(NpgsqlDataReader reader)
        {
            List<TaskClass> lista = new List<TaskClass>();
            while (await reader.ReadAsync())
            {
                lista.Add(creazioneTask(reader));
            }
            return lista;
        }

        static public async Task<List<MedalClass>> CreazioneListaMedals(NpgsqlDataReader reader)
        {
            List<MedalClass> lista = new List<MedalClass>();
            while (await reader.ReadAsync())
            {
                MedalClass tmp = new MedalClass();
                tmp.user = reader.GetString(usId);
                tmp.name = reader.GetString(medalNameId);
                tmp.quantity = reader.GetInt32(medalQuantityId);
                lista.Add(tmp);
            }
            return lista;
        }

        static public async Task<List<Log>> CreazioneListaLogs(NpgsqlDataReader reader)
        {
            List<Log> lista = new List<Log>();
            while (await reader.ReadAsync())
            {
                lista.Add(creazioneLog(reader));
            }
            return lista;
        }

        private async Task<Codes> CreateMedalTaskAsync(string username, string taskCategory, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@username, @category, @count)", medalTable), conn))
                {
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("category", taskCategory);
                    cmd.Parameters.AddWithValue("count", 1);
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                return Codes.MedalCreationError;
            }

        }

        internal TaskClass creazioneTask(NpgsqlDataReader reader)
        {
            TaskClass tmp = new TaskClass();
            tmp.user = reader.GetString(usId);
            tmp.name = reader.GetString(taskNameId);
            tmp.category = reader.GetString(taskCatId);
            tmp.date = reader.GetDateTime(taskDateId);
            tmp.description = reader.GetString(taskDescrId);
            tmp.verified = reader.GetBoolean(taskVerId);

            return tmp;
        }

        static Log creazioneLog(NpgsqlDataReader reader)
        {
            Log tmp = new Log();
            tmp.Username = reader.GetString(usId);
            tmp.Name = reader.GetString(taskNameId);
            tmp.Category = reader.GetString(taskCatId);
            tmp.date = reader.GetDateTime(taskDateId);
            tmp.verified = reader.GetBoolean(LogVerId);
            try
            {
                tmp.Picture = reader.GetInt16(LogIconId);
            }
            catch
            {
                tmp.Picture = -1;
            }
            try
            {
                tmp.Nickname = reader.GetString(LogNickId);
            }
            catch
            {
                tmp.Nickname = "";
            }
            return tmp;
        }

        private async Task<Boolean> VerificaEsistenzaTask(string username, string name, string category, string date, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND data='{4}'",
                    taskTable, username, category, name, date), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<int> VerificaEsistenzaTaskMedagliere(string username, string category, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT quantita FROM {0} WHERE username='{1}' AND nome='{2}'",
                    medalTable, username, category), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return reader.GetInt32(0);
                    else
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

    }
}
