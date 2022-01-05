# DesperateLambda
Metodi Desperate CLOUD

login : Effettua il login di un'utente.
u, p

register : Effettua la registrazione di un'utente
u, p, e


FAMILY
 - createFamily : Crea una famiglia con il nome fornito
    u, family (NOME)
    
 - addToFamily : Aggiunge l'utente alla famiglia solo se non è stato già invitato e solo se presente la richiesta.
    u, family  (ID)
    
 -  getTasksFamily : Ottiene tutte le tasks della famiglia (Possibilmente da modificare per ottenere solo le task verificate)
    u
 
 -  getMedalsFamily : Ottiene tutte le medaglie ottenute da ciascun membro della famiglia
    u
 
 -  quitFamily : Permette all'utente di uscire dalla famiglia
    u 
 
 -  getJoinRequestsFamily : Ottiene tutte le richieste effettuate all'utente per entrare nelle varie famiglie
    u
    
 -  requestJoinFamily : L'utente A effettua la richiesta per entrare nella famiglia all'utente B
    u (RICHIEDENTE) , u2 (JOINANTE)

 -  refuseJoinFamily : L'utente A riufiuta la richiesta effettuata dall'utente B riguardo all'u unirsi alla sua famiglia
    u, family (ID)

 -  getFamily : Ottiene tutti i membri appartenenti alla famiglia.
    u
 
 
 TASK
  - getNotVerifiedTask : Ottieni ogni Task non verificata dell'utente (Non ancora eseguita)
    u
    
  - getVerifiedTask : Ottieni ogni task verificata dell'utente (Tutte le task Eseguite)
    u
    
  - getTask : Ottieni tutte le task dell'utente
    u
    
  - addTask : Aggiungi la Task all'utente
    u, taskName, taskCategory, taskDescription, taskTime, taskDone, taskCustom
    
  - removeTask : Rimuovi la task dall'utente
    u, taskName, taskCategory, taskDescription, taskTime, taskDone, taskCustom
    
  - updateVerTask : Effettua l'update della verifica alla Task
    u, taskName, taskCategory, taskDescription, taskTime, taskDone, taskCustom
    
 MEDAL
 -  getMedal : Ottiene tutte le medaglie dell'utente
    u

LOG
 -  getLog : Ottiene il LOG della famiglia / dell'utente (Solo nel caso non avente una famiglia)
    u
    
 
