# Hey Thomas

### Velkommen til mit projekt

Jeg ville ønske vi kunne mødes under bedre forhold,
men jeg blev lidt stresset, og derfor ser her ud som der gør!

Anyways, på den lyse side er der også noget fedt at se, 
men jeg vil lige starte med at liste nogle af de ting du
kommer til at finde her, som kan virke lidt forvirrende.

### 1. Arkitekturen er lidt skiftende

Efterhånden som jeg kom længere og længere med opgaven, begyndte jeg
at blive mere og mere opmærksom på hvad jeg havde gang i.
Det betød, at jeg fik noget mere abstraktion på banen,
og fandt bedre måder at håndtere dependency injection på.
Det betyder til gengæld også, at article- og commentservice
først var skrevet meget simplistisk og uden nogen som helst lag
mellem databasen og api-controller (i know), og derfor
gennemgik en meget, meget hurtig refaktorering for ca. en halv time siden.
Med koffein og dødelige mængder nikotin i blodet, 
er det ikke til at sige om det fungerer, men i det mindste lod
EF mig migrate.

### 2. Hvor er mine logs/traces?
De skulle gerne blive printet i seq, men også der
var en farlig refaktorering, så det skal måske fikses.

### 3. Redundans er en (redun)dans på roser
Flere steder har jeg i panik indført enten regisrering af services
der er unødvendige, og i samme omgang ofte fjernet registreringer
der var. Mens jeg konceptuelt har forstået mine opgaver, 
så har der lige været nogle skavanker fordi jeg lige har
været distræt hist og her.

Det rammer nok de fleste punkter. Jeg får det opdateret løbende, 
så det i hvert fald kører forståeligt.

Håber du har en god dag,
Mikkel


ps: det kører på docker compose up, men det ved du helt sikkert i forvejen.
