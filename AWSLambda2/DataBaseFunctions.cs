using AWSLambda2;
using Codici;
using desperate_houseworks_project.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spazio
{

    class DataBaseFunctions
    {
        private string connString =
            String.Format("Host={0};Username={1};Password={2};Database={3};Port={4}",
                                   "151.24.14.109", "postgres", "123", "Datas", "5432");

        String loginTable = "login";
        String taskTable = "tasks";
        String familyTable = "famiglia";
        String medalTable = "medagliere";

        int usId = 0;
        int pwId = 1;
        int emId = 2;
        int loginVerId = 3;

        int taskCatId = 1;
        int taskNameId = 2;
        int taskDateId = 3;
        int taskDescrId = 4;
        int taskVerId = 5;
        int taskCustomId = 6;

        int familyId = 0;
        int familyNameId = 1;

        int medalNameId = 1;
        int medalQuantityId = 2;

        public async Task<Codes> LoginAsync(string username, string password)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------LOGIN " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE username='{1}'", loginTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        {
                            if (reader.GetString(usId).Equals(username) && reader.GetString(pwId).Equals(password))
                                if (reader.GetBoolean(loginVerId))
                                    return Codes.GenericSuccess;
                                else
                                    return Codes.LoginVerificationError;
                            else
                                return Codes.LoginUserPasswordError;
                        }
                    }

                    return Codes.LoginUserNotExists;
                }
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.LoginGenericError;
            }
        }

        public async Task<Codes> RegisterAsync(string username, string password, string email)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------REGISTER " + username + "----------------------");

                if (!MailVerificata(email))
                {
                    return Codes.RegistrationEmailNotValid;
                }

                if (VerificaEsistenzaUser(username, conn).Result)
                    return Codes.RegistrationUserExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE email='{1}'", loginTable, email), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        if (reader.GetString(emId).Equals(email))
                            return Codes.RegistrationEmailExistsError;
                }

                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@u, @p, @e, @v)", loginTable), conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    cmd.Parameters.AddWithValue("e", email);
                    cmd.Parameters.AddWithValue("v", false);
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.RegistrationError;
            }
        }


        internal async Task<List<TaskClass>> getTasksFamilyMethodAsync(string username, string family)
        {
            if (family == null)
                return GetEveryTaskMethodAsync(username).Result;

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- GetTasksFamily " + username + family + "----------------------");

                List<string> listaFamiliari = await GetListaFamiliari(family, conn);

                List<TaskClass> listaCompiti = new List<TaskClass>();

                foreach (string user in listaFamiliari)
                {
                    listaCompiti = listaCompiti.Concat(GetEveryTaskMethodAsync(user).Result).ToList();
                }

                return listaCompiti;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return null;
            }
        }


        internal async Task<Codes> AddToFamilyMethodAsync(string username, string family)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- AddToFamily " + username + family + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia={1} WHERE username='{2}'",
                    this.loginTable, family, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyAddUserError;
            }
        }

        internal async Task<Codes> CreateFamilyMethodAsync(string username, string family)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                int IdAttuale;

                Console.WriteLine("------------------- CreateFamily " + username + "----------------------");

                if (!VerificaEsistenzaUser(username, conn).Result)
                    return Codes.FamilyCreationError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE id=(Select max(id) from {0})",
                    this.familyTable, this.familyTable, username), conn))
                {
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            IdAttuale = reader.GetInt32(familyId) + 1;
                        else
                            IdAttuale = 1;
                    }
                }
                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@id, @n)", familyTable), conn))
                {
                    cmd.Parameters.AddWithValue("id", IdAttuale);
                    cmd.Parameters.AddWithValue("n", family);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia='{1}' WHERE username='{2}'",
                    this.loginTable, IdAttuale, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyCreationError;
            }
        }


        internal async Task<Codes> QuitFamilyMethodAsync(string username, string family)
        {
            if (family == null)
                return Codes.FamilyUserNotInFamily;
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- QuitFamily " + username + family + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia=null WHERE username='{2}'",
                    this.loginTable, family, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyQuitError;
            }
        }

        internal async Task<Dictionary<String, String>> User2Family(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- User2Family " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT id, nome FROM {0} JOIN {1} ON famiglia=id WHERE username='{2}'",
                    this.loginTable, this.familyTable, username), conn))
                {
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            {
                                d.Add("id", reader.GetInt32(0).ToString());
                                d.Add("name", reader.GetString(1));
                            }
                        }
                        return d;
                    }
                }
            }
            catch
            {
                Console.WriteLine("------------------- User not in a family " + username + "----------------------");
                return null;
            }
        }

        internal async Task<Codes> UpdateVerTasksMethodAsync(string username, string taskName,
                                                               string taskCategory, string taskDate,
                                                               string taskDescription, string taskDone)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------UpdateTask " + username + "----------------------");

                if (!VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, taskDescription, conn).Result)
                    return Codes.TaskDoesNotExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET verifica='true' WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND descrizione='{4}' AND data='{5}'",
                    this.taskTable, username, taskCategory, taskName, taskDescription, taskDate), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                int contatore = VerificaEsistenzaTaskMedagliere(username, taskCategory, conn).Result;

                if (contatore==0)
                    return CreateMedalTaskAsync(username, taskCategory, conn).Result;
                else
                {
                    await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET quantita = {1} WHERE username='{2}' AND nome='{3}'",
                    this.medalTable,contatore+1, username, taskCategory), conn))
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
                                                                string taskCategory, string taskDate,
                                                                string taskDescription, string taskDone)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------RemoveTask " + username + "----------------------");

                if (!VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, taskDescription, conn).Result)
                    return Codes.TaskDoesNotExistsError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE FROM {0}  WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND descrizione='{4}' AND data='{5}'",
                    this.taskTable, username, taskCategory, taskName, taskDescription, taskDate), conn))
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
                                                               string taskDescription, string taskDone,
                                                               string taskCustom)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------AddTask " + username + "----------------------");
                taskDescription = taskDescription.Replace("'", " ");
                taskName = taskName.Replace("'", " ");

                if (VerificaEsistenzaTask(username, taskName, taskCategory, taskDate, taskDescription, conn).Result)
                    return Codes.TaskExistsError;
                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@u, @c, @n, @t, @d, @v, @cu)", taskTable), conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("c", taskCategory);
                    cmd.Parameters.AddWithValue("n", taskName);
                    cmd.Parameters.AddWithValue("t", DateTime.Parse(taskDate));
                    cmd.Parameters.AddWithValue("d", taskDescription);
                    cmd.Parameters.AddWithValue("v", Boolean.Parse(taskDone));
                    cmd.Parameters.AddWithValue("cu", Boolean.Parse(taskCustom));
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

        internal async Task<List<MedalClass>> getMedalFamilyMethodAsync(string username, string family)
        {
            if (family == null)
                return GetMedalAsync(username).Result;
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- GetMedalsFamily " + username + family + "----------------------");

                List<string> listaFamiliari = await GetListaFamiliari(family, conn);

                List<MedalClass> listaCompiti = new List<MedalClass>();

                foreach (string user in listaFamiliari)
                {
                    listaCompiti = listaCompiti.Concat(GetMedalAsync(user).Result).ToList();
                }
                return listaCompiti;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return null;
            }
        }

        internal async Task<List<MedalClass>> GetMedalAsync(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------GetMedal " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE username='{1}'", medalTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaMedals(reader);
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<string>> GetListaFamiliari(string family, NpgsqlConnection conn)
        {
            List<String> listaFamiliari = new List<string>();
            await using (var cmd = new NpgsqlCommand(String.Format(
                "SELECT username from {0} WHERE famiglia={1}",
                this.loginTable, family), conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    listaFamiliari.Add(reader.GetString(0));
                }
            }
            return listaFamiliari;
        }

        private async Task<List<TaskClass>> CreazioneListaTasks(NpgsqlDataReader reader)
        {
            List<TaskClass> lista = new List<TaskClass>();
            while (await reader.ReadAsync())
            {
                TaskClass tmp = new TaskClass();
                tmp.user = reader.GetString(usId);
                tmp.name = reader.GetString(taskNameId);
                tmp.category = reader.GetString(taskCatId);
                tmp.date = reader.GetDateTime(taskDateId);
                tmp.description = reader.GetString(taskDescrId);
                tmp.verified = reader.GetBoolean(taskVerId);
                tmp.custom = reader.GetBoolean(taskCustomId);
                lista.Add(tmp);
            }
            return lista;
        }
        
        private async Task<List<MedalClass>> CreazioneListaMedals(NpgsqlDataReader reader)
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

        private async Task<Boolean> VerificaEsistenzaTask(string username, string name, string category, string date, string description, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND descrizione='{4}' AND data='{5}'",
                    this.taskTable, username, category, name, description, date), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return (reader.GetString(usId).Equals(username) &&
                                    reader.GetString(taskCatId).Equals(category) &&
                                    reader.GetString(taskNameId).Equals(name) &&
                                    reader.GetDateTime(taskDateId).Equals(DateTime.Parse(date)) &&
                                    reader.GetString(taskDescrId).Equals(description));
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<Codes> CreateMedalTaskAsync(string username, string taskCategory, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@username, @category, @count)", this.medalTable), conn))
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

        private async Task<int> VerificaEsistenzaTaskMedagliere(string username, string category, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT quantita FROM {0} WHERE username='{1}' AND nome='{2}'",
                    this.medalTable, username, category), conn))
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


        private async Task<Boolean> VerificaEsistenzaUser(string username, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}'",
                    this.taskTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return (reader.GetString(usId).Equals(username));
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        //Metodo per verificare la correttezza della mail usando Regex.
        private bool MailVerificata(string email)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            return match.Success;
        }
    }
}
