using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambda2
{
    class EmailFunctions
    {
        static string baseUrl = "https://vxnkf1zo3f.execute-api.eu-west-2.amazonaws.com/Beta/verify?token=";
        static string baseEmail = "desperatehouseworks@gmail.com";
        static string basePw = "Rei&Lela";



        internal async static Task<string> InviaEmailVerifica(string email, string user)
        {

            String token = EncDec.EncryptionHelper.Encrypt(DateTime.Now.ToString()+user);

            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.EnableSsl = true;
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential(baseEmail,basePw);

            smtp.Send(baseEmail, email.ToLower(), "Verifica account Desperate HouseWorks",
                "Questa e-mail ti è stata inviata in quanto ti sei registrato/a sull'applicazione DesperateHouseworks.\n" +
                          "Per verificare il tuo account clicca su questo link -> " + baseUrl + token);

            
            return token;
        }

        internal static void InviaEmailResetPassword(string email, int token)
        {

            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.EnableSsl = true;
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential(baseEmail, basePw);

            smtp.Send(baseEmail, email.ToLower(), "Reset della password DesperateHouseworks",
                "Questa e-mail ti è stata inviata in quanto hai richiesto il reset della password dell'applicazione.\n" +
                "Il codice da immettere è il seguente: " + token + "\n\n" +
                "Se non sei stato/a tu a richiedere modifica, ti invitiamo a modificare la password immediatamente.");
        }
    }
}
