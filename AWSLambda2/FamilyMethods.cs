using Codici;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace classi
{
    internal class FamilyMethods : DataBaseFunctions
    {

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
                    String.Format("Select login.username,categoria,tasks.nome,data,tasks.verifica, immagine, name FROM " +
                                            "login JOIN {0} tasks ON login.username = tasks.username " +
                                            "WHERE {1} AND data > '{2}' AND data < '{3}' " +
                                            "ORDER BY DATA DESC", modificaFamiglia1, modificaFamiglia2, startingPeriod, endingPeriod), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return await TaskMethods.CreazioneListaLogs(reader);
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
                    String.Format("SELECT username, immagine, name FROM {0} WHERE famiglia={1} AND NOT username='{2}' ",
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
                    String.Format("SELECT username_requesting, nome, {0}.id, immagine, name FROM {0} JOIN {1} ON {0}.id={1}.id JOIN {2} ON " +
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
                    return await TaskMethods.CreazioneListaMedals(reader);
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

                TaskMethods funzioniDatabase = new TaskMethods();
                string nicknameUserRequesting = await UserMethods.GetNickname(usernameRequesting, conn);
                string nicknameUser = await UserMethods.GetNickname(username, conn);
                await funzioniDatabase.AddTasksMethodAsync(usernameRequesting, nicknameUserRequesting + "  ha invitato " + nicknameUser + " ad unirsi alla famiglia", "Altro", DateTime.Now.ToString(), "", "true", usernameRequesting);

                Console.WriteLine("------------------- Inserita la richiesta di unione alla famiglia " + username + " -------------------");

                //string Evento = usernameRequesting + " ha invitato " + username + " ad unirsi alla vostra famiglia";
                //await CreateLogAsync(usernameRequesting, Evento, conn);

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
                    requestJoinFamilyTable, username, family), conn))
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
                    requestJoinFamilyTable, username, family), conn))

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.ReadAsync().Result)
                        if (reader.GetInt16(0) == 0)
                            return Codes.FamilyNoSuchJoinRequestError;
                }


                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia={1} WHERE username='{2}'",
                    loginTable, family, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE from {0} WHERE username='{1}' AND id={2}",
                    requestJoinFamilyTable, username, family), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }


                //Task che mostra l'unione dell'utente alla famiglia.

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT nome from {0} WHERE id={1}",
                    familyTable, family), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.ReadAsync().Result)
                    {
                        string familyName = reader.GetString(0);
                        TaskMethods funzioniDatabase = new TaskMethods();
                        await funzioniDatabase.AddTasksMethodAsync(username, "Benvenuto in " + familyName, "Altro", DateTime.Now.ToString(), "", "true", "");
                    }
                }


                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyAddUserError;
            }
        }

        internal async Task<Codes> UpdateFamilyNameAsync(string username, string name)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- CreateFamily " + username + "----------------------");

                //Si puo' rimuovere questo controllo, in quanto, se l'utente non esiste, il metodo che chiama questo metodo
                //Verificherà che l'utente non ha una famiglia, in quanto non esiste.
                //Lo lascio in quanto non si sa' mai

                if (!VerificaEsistenzaUser(username, conn).Result)
                    return Codes.FamilyCreationError;

                (await User2Family(username)).TryGetValue("id", out string familyID);

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET nome='{1}' WHERE id='{2}'",
                    familyTable, name, familyID), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                //Task che mostra la creazione della famiglia.

                TaskMethods funzioniDatabase = new TaskMethods();
                await funzioniDatabase.AddTasksMethodAsync(username, "Hai modificato il nome della famiglia :  " + name, "Altro", DateTime.Now.ToString(), "", "true", "");

                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyCreationError;
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
                    familyTable), conn))
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
                    loginTable, IdAttuale, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                //Task che mostra la creazione della famiglia.

                TaskMethods funzioniDatabase = new TaskMethods();
                await funzioniDatabase.AddTasksMethodAsync(username, "Hai creato " + family, "Altro", DateTime.Now.ToString(), "", "true","");

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
                //await CreateLogAsync(username, Evento, conn);

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "UPDATE {0} SET famiglia=null WHERE username='{2}'",
                    loginTable, family, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                //Aggiunta la task che mostra il momento in cui si è usciti dalla famiglia.

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT nome from {0} WHERE id={1}",
                    familyTable, family), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.ReadAsync().Result)
                    {
                        string familyName = reader.GetString(0);
                        TaskMethods funzioniDatabase = new TaskMethods();
                        await funzioniDatabase.AddTasksMethodAsync(username, "Hai lasciato " + familyName, "Altro", DateTime.Now.ToString(), "", "true", "");
                    }
                }


                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("------------------- CRASH " + username + "----------------------");
                return Codes.FamilyQuitError;
            }
        }


        private async Task<Boolean> VerificaEsistenzaRichiesta(string username, string family, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}' AND id={2})",
                    requestJoinFamilyTable, username, family), conn))
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

        private async Task<List<string>> GetListaFamiliari(string family, NpgsqlConnection conn)
        {
            List<String> listaFamiliari = new List<string>();
            await using (var cmd = new NpgsqlCommand(String.Format(
                "SELECT username from {0} WHERE famiglia={1}",
                loginTable, family), conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    listaFamiliari.Add(reader.GetString(0));
                }
            }
            return listaFamiliari;
        }

        private async Task<List<RequestClass>> CreazioneListaRequests(NpgsqlDataReader reader)
        {
            List<RequestClass> lista = new List<RequestClass>();
            while (await reader.ReadAsync())
            {
                RequestClass tmp = new RequestClass();
                tmp.Username = reader.GetString(0);
                tmp.familyName = reader.GetString(1);
                tmp.familyCode = reader.GetInt16(2);
                try
                {
                    tmp.Picture = reader.GetInt16(3);
                }
                catch
                {
                    tmp.Picture = -1;
                }
                tmp.Nickname = reader.GetString(4);
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

                try { tmp.Picture = reader.GetInt32(1); }
                catch { tmp.Picture = -1; }

                try { tmp.Nickname = reader.GetString(2); }
                catch { tmp.Nickname = ""; }
                lista.Add(tmp);
            }
            return lista;
        }



    }
}