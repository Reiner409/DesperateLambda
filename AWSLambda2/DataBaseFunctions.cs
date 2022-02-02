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
        static public string connString =
            String.Format("Host={0};Username={1};Password={2};Database={3};Port={4}",
                                   "151.24.29.32", "postgres", "123", "Datas", "5432");

        static public String loginTable = "login";
        static public String registerTable = "registrazione";
        static public String taskTable = "tasks";
        static public String familyTable = "famiglia";
        static public String requestJoinFamilyTable = "richiesta_famiglia";
        static public String medalTable = "medagliere";
        static public String logTable = "log_famiglia";
        static public String resetPasswordTable = "reset_password";

        static public int usId = 0;
        static public int pwId = 1;
        static public int emId = 2;
        static public int loginVerId = 3;
        static public int iconId = 5;
        static public int token_auth_Id = 7;
        static public int nickname_Id = 8;

        static public int taskCatId = 1;
        static public int taskNameId = 2;
        static public int taskDateId = 3;
        static public int taskDescrId = 4;
        static public int taskVerId = 5;

        static public int LogVerId = 4;
        static public int LogIconId = 5;
        static public int LogNickId = 6;

        static public int familyId = 0;
        static public int familyNameId = 1;

        static public int medalNameId = 1;
        static public int medalQuantityId = 2;

        static public async Task<Boolean> VerificaEsistenzaUser(string username, NpgsqlConnection conn)
        {
            Console.WriteLine("------------------- VerificaEsistenzaUser " + username + " -------------------");
            try
            {
                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT * FROM {0} WHERE username='{1}'",
                    loginTable, username), conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                    return (await reader.ReadAsync());
            }
            catch
            {
                Console.WriteLine("------------------- UserDoesNotExist " + username + " -------------------");
                return false;
            }
        }
        static public async Task<Dictionary<String, String>> User2Family(string username)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                Console.WriteLine("------------------- User2Family " + username + "----------------------");

                await using (var cmd = new NpgsqlCommand(String.Format(
                    "SELECT id, nome FROM {0} JOIN {1} ON famiglia=id WHERE username='{2}'",
                    loginTable, familyTable, username), conn))
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











        //private async Task<bool> CreateLogAsync(string username, string evento, NpgsqlConnection conn)
        //{
        //    string tmp;
        //    if (User2Family(username).Result.TryGetValue("id", out string family))
        //        tmp = family;
        //    else
        //        tmp = username;
        //    try
        //    {
        //        await using (var cmd = new NpgsqlCommand(String.Format("INSERT INTO {0}  VALUES (@id, @evento, @data)", logTable), conn))
        //        {
        //            cmd.Parameters.AddWithValue("id", tmp);
        //            cmd.Parameters.AddWithValue("evento", evento);
        //            cmd.Parameters.AddWithValue("data", DateTime.Now);
        //            await cmd.ExecuteNonQueryAsync();
        //        }
        //        Console.WriteLine("LOG CREATO CON SUCCESSO");
        //        return true;
        //    }
        //    catch
        //    {
        //        Console.WriteLine("CRASH SULLA CRAZIONE DEL LOG");
        //        return false;
        //    }

        //}



    }
}
