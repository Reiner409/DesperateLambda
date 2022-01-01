using System;
using System.Collections.Generic;
using System.Text;

namespace Codici
{
    enum Codes
    {

        //Success
        GenericSuccess = 200,


        //Login
        //LoginSuccesful = 200,
        LoginUserPasswordError = 400,
        LoginVerificationError = 401,
        LoginUserNotExists = 402,
        LoginGenericError=409,

        //Registration
        //RegistrationSuccesful = 210,
        RegistrationError = 410,
        RegistrationUserExistsError=411,
        RegistrationEmailExistsError = 412,
        RegistrationEmailNotValid = 413,

        //Task
        //TaskSuccess = 220,
        TaskUsernameError = 420,
        TaskGenericError =421,
        TaskExistsError = 422,
        TaskDoesNotExistsError = 423,
        TaskAddError = 424,
        TaskRemoveError = 425,
        TaskWrongOperation = 426,
        TaskGetNotVerifiedError = 427,

        //Family
        FamilyCreationError = 430,
        FamilyAddUserError = 431,
        FamilyUserAlreadyInFamily = 432,
        FamilyUserNotInFamily = 433,
        FamilyQuitError = 434,
        JoinFamilyError = 435,
        JoinFamilyRequestAlreadyExistsError = 436,

        //Medal
        MedalCreationError = 440,
        MedalUpdateError = 441,

         //GenericErrors
        RequestNotFound = 490,
        DatabaseConnectionError = 491,
        ToBeAdded=498,
        GenericError = 499,
    }

    class GestioneCodici
    {
        public string CodeToText(int c)
        {
            switch (c)
            {
                //GenericSuccess
                case 200:
                    return "Generic - Succesful operation";
                    
                //Login
                //case 200:
                //    return "Login - Succesful login";
                case 400:
                    return "Login - Wrong username/password";
                case 401:
                    return "Login - Missing verification";
                case 402:
                    return "Login - User does not exists";
                case 409:
                    return "Login - Generic Error";

                //Registrazione
                case 210:
                    return "Registration - Succesful registration";
                case 410:
                    return "Registration - Something went wrong with registration";
                case 411:
                    return "Registration - User already exists";
                case 412:
                    return "Registration - Email already in use";
                case 413:
                    return "Registration - Email not valid";

                //Task
                case 420:
                    return "Task - Username doesn't exists or not verified";
                case 421:
                    return "Task - Generic error";
                case 422:
                    return "Task - Add - Task already exists";
                case 423:
                    return "Task - Task does not exist";
                case 424:
                    return "Task - Add/Update - Error";
                case 425:
                    return "Task - Remove - Error";
                case 426:
                    return "Task - Wrong Task operation";
                case 427:
                    return "Task - Get Not Verified - Returned null. Error or Empty";

                //Family
                case 430:
                    return "Family - Creation failed";
                case 431:
                    return "Family - Can't add user to Family";
                case 432:
                    return "Family - User already in a Family";
                case 433:
                    return "Family - User not in a Family";
                case 434:
                    return "Family - Quit Family Error";
                case 435:
                    return "Family - Join Family Error";
                case 436:
                    return "Family - Pending Request already Exists";
                    
                //Medal
                case 440:
                    return "Medal - Creation failed";
                case 441:
                    return "Medal - Update failed";

                //GenericError
                case 490:
                    return "RequestNotFound";
                case 491:
                    return "Connection with Database not established";
                case 498:
                    return "To Be Added";
                case 499:
                    return "Something went wrong";

                //Default
                default:
                    return "Request not found.";
            }
        }
    }
}
