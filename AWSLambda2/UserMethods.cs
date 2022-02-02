using AWSLambda2;
using Codici;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace classi
{
    class UserMethods : DataBaseFunctions
    {

        public async Task<Tuple<Codes, Dictionary<String, String>>> LoginAsync(string username, string password, string tokenNotifications)
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
                                    catch { diz.Add("Token", await RefreshAuthToken(username)); }

                                    await UpdateNotificationsToken(username, tokenNotifications);

                                    try { diz.Add("Nickname", reader.GetString(nickname_Id).ToString()); }
                                    catch { diz.Add("Nickname", ""); }

                                    if (User2Family(username).Result.TryGetValue("name", out string fam))
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

        public async Task<Tuple<Codes, Dictionary<String, String>>> LoginTokenAsync(string token, string tokenNotifications)
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

                        if (User2Family(username).Result.TryGetValue("name", out string fam))
                            diz.Add("Family", fam);

                        try { diz.Add("Nickname", reader.GetString(nickname_Id).ToString()); }
                        catch { diz.Add("Nickname", ""); }

                        await UpdateNotificationsToken(username, tokenNotifications);

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
                String token = GenerateTokenVerifyUser(username);
                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET token_auth='{1}' WHERE username='{2}'",
                        loginTable, token, username), conn))
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

        public async Task UpdateNotificationsToken(string username, string token)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET token_notifiche='{1}' WHERE username='{2}'",
                        loginTable, token, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Couldn't update Notification token for " + username + " token: " + token);
            }

        }

        public static async Task<String> GetUserNotificationToken(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------LOGIN " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT token_notifiche FROM {0} WHERE username='{1}'", loginTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                        return reader.GetString(0);
                return "";
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
                        loginTable, icon, user), conn))
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

        internal async Task<Codes> UpdateUserPasswordAsync(string username, string password)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                password = EncDec.EncryptionHelper.Encrypt(username, password);

                await using (var cmd = new NpgsqlCommand(String.Format(
                        "UPDATE {0} SET password='{1}' WHERE username='{2}'",
                        loginTable, password, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE FROM {0} WHERE username='{1}'",
                    resetPasswordTable, username), conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                return Codes.UpdateUserPasswordError;
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
                        loginTable, name, user), conn))
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
                if (await VerificaEsistenzaUser(username, conn))
                    return Codes.RegistrationUserExistsError;

                string token = await EmailFunctions.InviaEmailVerifica(email, username);

                await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@us, @pw, @em, @ver,@fam,@pic,@tok_not,@tok_auth,@nick)", loginTable), conn))
                {
                    cmd.Parameters.AddWithValue("us", username);
                    cmd.Parameters.AddWithValue("pw", EncDec.EncryptionHelper.Encrypt(username, password));
                    cmd.Parameters.AddWithValue("em", email);
                    cmd.Parameters.AddWithValue("ver", false);
                    cmd.Parameters.AddWithValue("fam", DBNull.Value);
                    cmd.Parameters.AddWithValue("pic", -1);
                    cmd.Parameters.AddWithValue("tok_not", DBNull.Value);
                    cmd.Parameters.AddWithValue("tok_auth", DBNull.Value);
                    cmd.Parameters.AddWithValue("nick", username);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd2 = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@us, @tok)", registerTable), conn))
                {
                    cmd2.Parameters.AddWithValue("us", username);
                    cmd2.Parameters.AddWithValue("tok", token);
                    await cmd2.ExecuteNonQueryAsync();
                }
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.RegistrationError;
            }
        }

        public async Task<Codes> ResetPasswordFirstStepAsync(string email)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------RESETPASSWORD - SEND EMAIL " + email + "----------------------");

                string username = "";

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT username FROM {0} WHERE email='{1}'", loginTable, email), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        username = reader.GetString(0).ToString();
                }

                int token = GenerateTokenResetPassword();

                //Inserisci un nuovo token se non presente
                try
                {
                    await using (var cmd2 = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@us, @tok)", resetPasswordTable), conn))
                    {
                        cmd2.Parameters.AddWithValue("us", username);
                        cmd2.Parameters.AddWithValue("tok", token);
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }
                catch
                {
                    //Se già presente, lo aggiorno
                    await using (var cmd2 = new NpgsqlCommand(String.Format("UPDATE {0} SET token={1}  WHERE username='{2}' ", 
                        resetPasswordTable,token, username), conn))
                    {
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }

                EmailFunctions.InviaEmailResetPassword(email, token);

                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH " + email + "----------------------");
                return Codes.RegistrationError;
            }
        }

        public async Task<Codes> VerifyResetPassword(string username, string tokenToVerify)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------RESETPASSWORD - VERIFY " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format("SELECT token FROM {0} WHERE username='{1}'", resetPasswordTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string token = reader.GetInt32(0).ToString();
                        if (tokenToVerify.Equals(token))
                        {
                            return Codes.GenericSuccess;
                        }
                    }
                }

                return Codes.UpdatePasswordWrongToken;
            }

            catch
            {
                Console.WriteLine("-------------------CRASH " + username + "----------------------");
                return Codes.RegistrationError;
            }
        }

        private string GenerateTokenVerifyUser(string user)
        {
            return EncDec.EncryptionHelper.Encrypt(DateTime.Now.ToString(), user);
        }

        private int GenerateTokenResetPassword()
        {
            return new Random().Next(100000, 999999);
        }

        internal async Task<Codes> VerifyUserAsync(string token)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("-------------------VerifyUser----------------------");

                string username = verificaEsistenzaToken(token, conn).Result;
                if (username == null)
                    return Codes.GenericError;

                await VerifyUser(conn, username);
                await RemoveUserFromRegisterTable(conn, username);
                return Codes.GenericSuccess;
            }
            catch
            {
                Console.WriteLine("-------------------CRASH----------------------");
                return Codes.LoginVerificationError;
            }
        }

        private async Task VerifyUser(NpgsqlConnection conn, string username)
        {
            await using (var cmd = new NpgsqlCommand(String.Format(
                "UPDATE {0} SET verifica='true' WHERE username='{1}'",
                loginTable, username), conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {

                }
        }

        private async Task RemoveUserFromRegisterTable(NpgsqlConnection conn, string username)
        {
            await using (var cmd = new NpgsqlCommand(String.Format(
                    "DELETE FROM {0} WHERE username='{1}'",
                    registerTable, username), conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {

                }
        }

        private async Task<string> verificaEsistenzaToken(string token, NpgsqlConnection conn)
        {
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE token='{1}'",
                    registerTable, token), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    return reader.GetString(0);
                }
            }
            catch
            {
                return null;
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
