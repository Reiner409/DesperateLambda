using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Codici;
using desperate_houseworks_project.Models;
using spazio;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda2
{
    public class Function
    {


        readonly string username = "u";
        readonly string password = "p";
        readonly string email = "e";

        readonly string family = "family";
        readonly string usernameToJoinFamily = "u2";

        readonly string taskName = "taskName";
        readonly string taskCategory = "taskCategory";
        readonly string taskDescription = "taskDescription";
        readonly string taskTime = "taskTime";
        readonly string taskDone = "taskDone";
        readonly string taskCustom = "taskCustom";

        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                request.QueryStringParameters.TryGetValue("operation", out string operation);

                if (operation.Equals("login"))
                    return Login(request, context);
                if (operation.Equals("register"))
                    return Register(request, context);

                if (operation.EndsWith("Family"))
                    return Family(request, context, operation);

                if (operation.EndsWith("Task"))
                    return Task(request, context, operation);
                
                if (operation.EndsWith("Medal"))
                    return Medal(request, context, operation);

                return Response(Codes.RequestNotFound);
            }
            catch (Exception)
            {
                return Response(Codes.GenericError);
            }
        }


        private APIGatewayProxyResponse Login(APIGatewayProxyRequest request, ILambdaContext context)

        {

            IDictionary<string, string> dizionario = request.QueryStringParameters;
            dizionario.TryGetValue(this.username, out string userID);
            dizionario.TryGetValue(this.password, out string password);


            DataBaseFunctions funzioniDatabase = new DataBaseFunctions();
            try
            {

                Task<Codes> codice = funzioniDatabase.LoginAsync(userID, password);

                return Response(codice.Result);
            }
            catch
            {
                return Response(Codes.DatabaseConnectionError);
            }
        }

        private APIGatewayProxyResponse Register(APIGatewayProxyRequest request, ILambdaContext context)
        {
            IDictionary<string, string> dizionario = request.QueryStringParameters;
            dizionario.TryGetValue(this.username, out string userID);
            dizionario.TryGetValue(this.password, out string password);
            dizionario.TryGetValue(this.email, out string email);

            DataBaseFunctions funzioniDatabase = new DataBaseFunctions();
            Task<Codes> codice = funzioniDatabase.RegisterAsync(userID, password, email);

            return Response(codice.Result);
        }

        private APIGatewayProxyResponse Family(APIGatewayProxyRequest request, ILambdaContext context, string operation)
        {
            DataBaseFunctions funzioniDatabase = new DataBaseFunctions();
            IDictionary<string, string> dizionario = request.QueryStringParameters;
            dizionario.TryGetValue(this.username, out string username);
            string family;

            if (operation.Equals("createFamily"))
            {
                if (funzioniDatabase.User2Family(username).Result.Count != 0)
                    return Response(Codes.FamilyUserAlreadyInFamily);
                dizionario.TryGetValue(this.family, out family);
                return Response(funzioniDatabase.CreateFamilyMethodAsync(username, family).Result);
            }
            

            if (operation.Equals("addToFamily"))
            {
                dizionario.TryGetValue(this.family, out family);
                return Response(funzioniDatabase.AddToFamilyMethodAsync(username, family).Result);
            }
            
            if (operation.Equals("getJoinRequestsFamily"))
            {
                return Response(funzioniDatabase.GetJoinRequestsFamilyAsync(username).Result);
            }
            
            try
            {
                funzioniDatabase.User2Family(username).Result.TryGetValue("id", out family);
            }
            catch
            {
                family = null;
            }

            switch (operation)
            {
                case "getTasksFamily":
                    return Response(funzioniDatabase.GetTasksFamilyMethodAsync(username, family).Result);
                case "getMedalsFamily":
                    return Response(funzioniDatabase.getMedalFamilyMethodAsync(username, family).Result);
                case "quitFamily":
                    return Response(funzioniDatabase.QuitFamilyMethodAsync(username, family).Result);

                case "requestJoinFamily":
                    string usernameToJoinFamily;
                    dizionario.TryGetValue(this.usernameToJoinFamily, out usernameToJoinFamily);
                    return Response(funzioniDatabase.RequestJoinFamilyAsync(usernameToJoinFamily, username, family).Result);

                default:
                    return Response(Codes.RequestNotFound);
            }
        }


        //Creazione di Tasks all'interno della tabella DisperateTasks
        private APIGatewayProxyResponse Task(APIGatewayProxyRequest request, ILambdaContext context, string operation)
        {

            DataBaseFunctions funzioniDatabase = new DataBaseFunctions();
            IDictionary<string, string> dizionario = request.QueryStringParameters;
            dizionario.TryGetValue(this.username, out string username);

            if (operation.Equals("getNotVerifiedTask"))
                return Response((funzioniDatabase.GetVerifiedTaskMethodAsync(username, "false").Result));
            if (operation.Equals("getVerifiedTask"))
                return Response((funzioniDatabase.GetVerifiedTaskMethodAsync(username, "true").Result));
            if (operation.Equals("getTask"))
                return Response((funzioniDatabase.GetEveryTaskMethodAsync(username).Result));

            dizionario.TryGetValue(this.taskName, out string taskN);
            dizionario.TryGetValue(this.taskCategory, out string taskC);
            dizionario.TryGetValue(this.taskTime, out string taskT);
            dizionario.TryGetValue(this.taskDescription, out string taskDescr);
            dizionario.TryGetValue(this.taskDone, out string taskDone);
            dizionario.TryGetValue(this.taskCustom, out string taskPers);

            switch (operation)
            {
                case "addTask":
                    return Response((funzioniDatabase.AddTasksMethodAsync(username, taskN, taskC, taskT, taskDescr, taskDone, taskPers).Result));
                case "removeTask":
                    return Response((funzioniDatabase.RemoveTasksMethodAsync(username, taskN, taskC, taskT, taskDescr, taskDone).Result));
                case "updateVerTask":
                    return Response((funzioniDatabase.UpdateVerTasksMethodAsync(username, taskN, taskC, taskT, taskDescr, taskDone).Result));
                default: return Response(Codes.GenericError);
            }
        }

        private APIGatewayProxyResponse Medal(APIGatewayProxyRequest request, ILambdaContext context, string operation)
        {
            DataBaseFunctions funzioniDatabase = new DataBaseFunctions();
            IDictionary<string, string> dizionario = request.QueryStringParameters;
            dizionario.TryGetValue(this.username, out string username);
            switch (operation)
            {
                case "getMedal":
                    return Response(funzioniDatabase.GetMedalAsync(username).Result);
                default:
                    return Response(Codes.GenericError);
            }
        }

        private APIGatewayProxyResponse Response(List<RequestClass> lista)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize<List<RequestClass>>(lista)
            };
        }
        //Tutte le tasks
        private APIGatewayProxyResponse Response(List<TaskClass> lista)
        {
            if (lista.Equals(null))
                return Response(Codes.TaskGetNotVerifiedError);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize<List<TaskClass>>(lista)
                
            };

        }
        
        //Tutte le medaglie
        private APIGatewayProxyResponse Response(List<MedalClass> lista)
        {
            if (lista.Equals(null))
                return Response(Codes.TaskGetNotVerifiedError);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize<List<MedalClass>>(lista)
                
            };

        }

        //Risposte ai codici
        private APIGatewayProxyResponse Response(Codes code)
        {

            GestioneCodici gestione = new GestioneCodici();
            int codice = (int)code;

            return new APIGatewayProxyResponse
            {
                StatusCode = codice,
                Body = gestione.CodeToText(codice)
            };
        }
    }
}
