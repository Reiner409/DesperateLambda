using AWSLambda2;
using Codici;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace classi
{

    class DataBaseFunctions
    {
        private string connString =
            String.Format("Host={0};Username={1};Password={2};Database={3};Port={4}",
                                   "151.24.29.32", "postgres", "123", "Datas", "5432");

        String loginTable = "login";
        String taskTable = "tasks";
        String familyTable = "famiglia";
        String requestJoinFamilyTable = "richiesta_famiglia";
        String medalTable = "medagliere";
        String logTable = "log_famiglia";

        int usId = 0;
        int pwId = 1;
        int emId = 2;
        int loginVerId = 3;
        int iconId = 5;
        int token_auth_Id = 7;

        int taskCatId = 1;
        int taskNameId = 2;
        int taskDateId = 3;
        int taskDescrId = 4;
        int taskVerId = 5;

        int LogVerId = 4;
        int LogIconId = 5;

        int familyId = 0;
        int familyNameId = 1;

        int medalNameId = 1;
        int medalQuantityId = 2;

        public async Task<Tuple<Codes, Dictionary<String, String>>> LoginAsync(string username, string password)
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
                            if (reader.GetString(usId).Equals(username) && reader.GetString(pwId).Equals(EncDec.EncryptionHelper.Encrypt(username, password)))
                                if (reader.GetBoolean(loginVerId))
                                {
                                    Dictionary<String, String> diz = new Dictionary<string, string>();

                                    try { diz.Add("Picture", reader.GetInt32(iconId).ToString()); }
                                    catch { diz.Add("Picture", "-1"); }

                                    try { diz.Add("Token", reader.GetString(token_auth_Id).ToString()); }
                                    catch { diz.Add("Token", await this.RefreshAuthToken(username)); }

                                    if (this.User2Family(username).Result.TryGetValue("name", out string fam))
                                        diz.Add("Family", fam);
                                    return Tuple.Create(Codes.GenericSuccess, diz);
                                }
                                else
                                    return Tuple.Create(Codes.LoginVerificationError, new Dictionary<String, String>());
                            else
                                return Tuple.Create(Codes.LoginUserPasswordError, new Dictionary<String, String>());
                        }
                    }

                    return Tuple.Create(Codes.LoginUserNotExists, new Dictionary<String, String>());
                }
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Tuple.Create(Codes.LoginGenericError, new Dictionary<String, String>());
            }
        }

        public async Task<Tuple<Codes, Dictionary<String, String>>> Login2Async(string token)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------LOGIN----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE token_auth='{1}'", loginTable, token), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {

                        Dictionary<String, String> diz = new Dictionary<string, string>();

                        string username = reader.GetString(usId).ToString();

                        try { diz.Add("Picture", reader.GetInt32(iconId).ToString()); }
                        catch { diz.Add("Picture", "-1"); }

                        if (this.User2Family(username).Result.TryGetValue("name", out string fam))
                            diz.Add("Family", fam);
                        return Tuple.Create(Codes.GenericSuccess, diz);
                    }
                    return Tuple.Create(Codes.LoginTokenNotExists, new Dictionary<String, String>());
                }
            }
            catch
            {
                Console.WriteLine("-------------------CRASH ----------------------");
                return Tuple.Create(Codes.LoginGenericError, new Dictionary<String, String>());
            }
        }

        public async Task<String> RefreshAuthToken(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                String token = generateToken(username);
                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET token_auth='{1}' WHERE username='{2}'",
                        this.loginTable, token, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return token;
            }
            catch
            {
                return "";
            }

        }

        internal async Task<Codes> UpdateUserIconAsync(string user, string icon)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                if (!VerificaEsistenzaUser(user, conn).Result)
                    throw new Exception();

                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET immagine={1} WHERE username='{2}'",
                        this.loginTable, icon, user), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                return Codes.UpdateUserIconError;
            }
        }

        internal async Task<Codes> UpdateUserNameAsync(string user, string name)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                if (!VerificaEsistenzaUser(user, conn).Result)
                    throw new Exception();

                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET name='{1}' WHERE username='{2}'",
                        this.loginTable, name, user), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                return Codes.UpdateUserNameError;
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
                    return Codes.RegistrationEmailNotValid;
                if (await VerificaEsistenzaMail(email, conn))
                    return Codes.RegistrationEmailExistsError;
                if (VerificaEsistenzaUser(username, conn).Result)
                    return Codes.RegistrationUserExistsError;

                string token = await EmailFunctions.InviaEmailVerifica(email, username);

                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@us, @pw, @em, @ver,@fam,@pic,@tok)", loginTable), conn))
                {
                    cmd.Parameters.AddWithValue("us", username);
                    cmd.Parameters.AddWithValue("pw", EncDec.EncryptionHelper.Encrypt(username, password));
                    cmd.Parameters.AddWithValue("em", email);
                    cmd.Parameters.AddWithValue("ver", false);
                    cmd.Parameters.AddWithValue("fam", DBNull.Value);
                    cmd.Parameters.AddWithValue("pic", -1);
                    cmd.Parameters.AddWithValue("tok", token);
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

        private string generateToken(string user)
        {
            return EncDec.EncryptionHelper.Encrypt(DateTime.Now.ToString(), user);
        }

        internal async Task<Codes> VerifyUserAsync(string token)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------VerifyUser----------------------");

                if (!await verificaEsistenzaToken(token, conn))
                    return Codes.GenericError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET verifica='true', token=NULL WHERE token='{1}'",
                    this.loginTable, token), conn))

                await using (var reader = await cmd.ExecuteReaderAsync())
                    return Codes.GenericSuccess;

            }
            catch
            {
                Console.WriteLine("-------------------CRASH----------------------");
                return Codes.LoginVerificationError;
            }
        }



        //internal async Task<List<LogClass>> GetLog(string user, int numero = 10)
        //{
        //    try
        //    {
        //        if (User2Family(user).Result.TryGetValue("id", out string family))
        //            user = family;

        //        await using var conn = new NpgsqlConnection(connString);
        //        await conn.OpenAsync();
        //        List<LogClass> lista = new List<LogClass>();

        //        Console.WriteLine("-------------------GetLastLogs " + user + "----------------------");

        //        await using (var cmd = new NpgsqlCommand(String.Format("SELECT evento, data FROM {0} WHERE id='{1}' ORDER BY data DESC LIMIT {2}", logTable, user, numero), conn))
        //        await using (var reader = await cmd.ExecuteReaderAsync())
        //        {
        //            while (await reader.ReadAsync())
        //            {
        //                LogClass tmp = new LogClass();
        //                tmp.log = reader.GetString(0);
        //                tmp.Data = reader.GetDateTime(1);
        //                lista.Add(tmp);
        //            }
        //            return lista;
        //        }
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}



        internal async Task<List<Log>> GetTasksFamilyMethodAsync(string username, string family, string startingPeriod, string endingPeriod)
        {
            String modificaFamiglia1 = " ";
            String modificaFamiglia2;

            if (family == null)
                modificaFamiglia2 = String.Format("login.username = '{0}'", username);
            else
            {
                modificaFamiglia1 = " famiglia ON famiglia = id JOIN ";
                modificaFamiglia2 = String.Format("famiglia = {0}", family);
            }

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- GetTasksFamily " + username + family + "----------------------");

                await using (var cmd = new NpgsqlCommand(
                    String.Format("Select login.username,categoria,tasks.nome,data,tasks.verifica, immagine FROM " +
                                            "login JOIN {0} tasks ON login.username = tasks.username " +
                                            "WHERE {1} AND data > '{2}' AND data < '{3}' " +
                                            "ORDER BY DATA DESC", modificaFamiglia1, modificaFamiglia2, startingPeriod, endingPeriod), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaLogs(reader);
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return null;
            }
        }


        internal async Task<List<FamilyMember>> GetFamily(string username, string family)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------GetFamily" + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(
                    String.Format("SELECT username, immagine FROM {0} WHERE famiglia={1} AND NOT username='{2}' ",
                    loginTable, family, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaFamilyMember(reader);
            }
            catch
            {
                return null;
            }
        }

        internal async Task<List<RequestClass>> GetJoinRequestsFamilyAsync(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------GetJoinRequestsFamily" + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(
                    String.Format("SELECT username_requesting, nome, {0}.id, immagine FROM {0} JOIN {1} ON {0}.id={1}.id JOIN {2} ON " +
                    "username_requesting={2}.username  WHERE {1}.username='{3}'",
                    familyTable, requestJoinFamilyTable, loginTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await CreazioneListaRequests(reader);
            }
            catch
            {
                return null;
            }
        }

        internal async Task<Codes> RequestJoinFamilyAsync(string username, string usernameRequesting, string family)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- RequestJoinFamily " + username + "----------------------");

                if (!VerificaEsistenzaUser(username, conn).Result || family == null)
                    return Codes.JoinFamilyError;

                Console.WriteLine("------------------- SuperatoTestEsistenzaUser " + username + " -------------------");

                if (VerificaEsistenzaRichiesta(username, family, conn).Result)
                    return Codes.JoinFamilyRequestAlreadyExistsError;

                Console.WriteLine("------------------- SuperatoTestEsistenzaRichiesta " + username + " -------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0} VALUES (@u, @id, @ureq)", requestJoinFamilyTable), conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("id", int.Parse(family));
                    cmd.Parameters.AddWithValue("ureq", usernameRequesting);
                    await cmd.ExecuteNonQueryAsync();
                }

                Console.WriteLine("------------------- Inserita la richiesta di unione alla famiglia " + username + " -------------------");

                string Evento = usernameRequesting + " ha invitato " + username + " ad unirsi alla vostra famiglia";
                await CreateLogAsync(usernameRequesting, Evento, conn);

                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.JoinFamilyError;
            }
        }

        internal async Task<Codes> RefuseJoinFamilyAsync(string username, string family)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- RefuseJoinFamily " + username + " " + family + " ----------------------");

                if (!VerificaEsistenzaUser(username, conn).Result)
                    return Codes.FamilyCreationError;

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE FROM {0} WHERE username='{1}' AND id={2}",
                    this.requestJoinFamilyTable, username, family), conn))
                {
                    await using (var reader = await cmd.ExecuteReaderAsync())
                        return Codes.GenericSuccess;
                }
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyJoinRequestDeletionError;
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
                    "SELECT Count(*) as conteggio from {0} WHERE username='{1}' AND id={2}",
                    this.requestJoinFamilyTable, username, family), conn))

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.ReadAsync().Result)
                        if (reader.GetInt16(0) == 0)
                            return Codes.FamilyNoSuchJoinRequestError;
                }


                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia={1} WHERE username='{2}'",
                    this.loginTable, family, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE from {0} WHERE username='{1}' AND id={2}",
                    this.requestJoinFamilyTable, username, family), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                string Evento = username + " si è aggiunto alla famiglia!";
                await CreateLogAsync(username, Evento, conn);

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
                    this.familyTable), conn))
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

                string Evento = username + " ha abbandonato la famiglia.";
                await CreateLogAsync(username, Evento, conn);

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
                    "UPDATE {0} SET verifica='true' WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND AND data='{4}'",
                    this.taskTable, username, taskCategory, taskName, taskDate), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                int contatore = VerificaEsistenzaTaskMedagliere(username, taskCategory, conn).Result;

                string Evento = "L'utente " + username + " ha completato il seguente compito: " + taskName;
                await CreateLogAsync(username, Evento, conn);

                if (contatore == 0)
                    return CreateMedalTaskAsync(username, taskCategory, conn).Result;
                else
                {
                    await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET quantita = {1} WHERE username='{2}' AND nome='{3}'",
                    this.medalTable, contatore + 1, username, taskCategory), conn))
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
                    this.taskTable, username, taskCategory, taskName, taskDate), conn))
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

        internal async Task<List<MedalClass>> GetMedalFamilyMethodAsync(string username, string family)
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
                lista.Add(creazioneTask(reader));
            }
            return lista;
        }
        private async Task<List<Log>> CreazioneListaLogs(NpgsqlDataReader reader)
        {
            List<Log> lista = new List<Log>();
            while (await reader.ReadAsync())
            {
                lista.Add(creazioneLog(reader));
            }
            return lista;
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

        internal Log creazioneLog(NpgsqlDataReader reader)
        {
            Log tmp = new Log();
            FamilyMember tmpMember = new FamilyMember();
            tmpMember.Username = reader.GetString(usId);
            tmp.Name = reader.GetString(taskNameId);
            tmp.Category = reader.GetString(taskCatId);
            tmp.date = reader.GetDateTime(taskDateId);
            tmp.verified = reader.GetBoolean(LogVerId);
            try
            {
                tmpMember.Picture = reader.GetInt16(LogIconId);
            }
            catch
            {
                tmpMember.Picture = -1;
            }
            tmp.User = tmpMember;
            return tmp;
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

        private async Task<List<RequestClass>> CreazioneListaRequests(NpgsqlDataReader reader)
        {
            List<RequestClass> lista = new List<RequestClass>();
            while (await reader.ReadAsync())
            {
                RequestClass tmp = new RequestClass();
                FamilyMember user = new FamilyMember();
                user.Username = reader.GetString(0);
                tmp.familyName = reader.GetString(1);
                tmp.familyCode = reader.GetInt16(2);
                try
                {
                    user.Picture = reader.GetInt16(3);
                }
                catch
                {
                    user.Picture = -1;
                }
                lista.Add(tmp);
            }
            return lista;
        }
        private async Task<List<FamilyMember>> CreazioneListaFamilyMember(NpgsqlDataReader reader)
        {
            List<FamilyMember> lista = new List<FamilyMember>();
            while (await reader.ReadAsync())
            {
                FamilyMember tmp = new FamilyMember();
                tmp.Username = reader.GetString(0);
                try
                {
                    tmp.Picture = reader.GetInt32(1);
                }
                catch
                {
                    tmp.Picture = -1;
                }
                lista.Add(tmp);
            }
            return lista;
        }

        private async Task<Boolean> verificaEsistenzaToken(string token, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE token='{1}'",
                    this.loginTable, token), conn))
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

        private async Task<Boolean> VerificaEsistenzaTask(string username, string name, string category, string date, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}' AND categoria='{2}' AND nome='{3}' AND data='{4}'",
                    this.taskTable, username, category, name, date), conn))
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

        private async Task<bool> CreateLogAsync(string username, string evento, NpgsqlConnection conn)
        {
            string tmp;
            if (User2Family(username).Result.TryGetValue("id", out string family))
                tmp = family;
            else
                tmp = username;
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@id, @evento, @data)", this.logTable), conn))
                {
                    cmd.Parameters.AddWithValue("id", tmp);
                    cmd.Parameters.AddWithValue("evento", evento);
                    cmd.Parameters.AddWithValue("data", DateTime.Now);
                    await cmd.ExecuteNonQueryAsync();
                }
                Console.WriteLine("LOG CREATO CON SUCCESSO");
                return true;
            }
            catch
            {
                Console.WriteLine("CRASH SULLA CRAZIONE DEL LOG");
                return false;
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
            Console.WriteLine("------------------- VerificaEsistenzaUser " + username + " -------------------");
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}'",
                    this.loginTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return (await reader.ReadAsync());
            }
            catch
            {
                Console.WriteLine("------------------- UserDoesNotExist " + username + " -------------------");
                return false;
            }
        }
        private async Task<Boolean> VerificaEsistenzaRichiesta(string username, string family, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}' AND id={2})",
                    this.requestJoinFamilyTable, username, family), conn))
                {
                    await using (var reader = await cmd.ExecuteReaderAsync())
                        return await reader.ReadAsync();
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> VerificaEsistenzaMail(string email, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format("SELECT * FROM {0} WHERE email='{1}'", loginTable, email), conn))
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

        //Metodo per verificare la correttezza della mail usando Regex.
        private bool MailVerificata(string email)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            return match.Success;
        }
    }
}
