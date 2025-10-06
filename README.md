# Hey Thomas

## Velkommen til mit projekt

 Det her er en refaktorering per den 6. oktober.
Gransk tidligere commits for den fuldstændige velkomst.

### Patch notes october 6th part deux
Because of a lack of a stable build, these two updates hit simultaneously.

What's new and old since this morning?
- Release turns out to be relatively stable.
- Seq log available at ```localhost:5342```
- Zipkin traces at ```localhost:9411```
- Some classes have had redundancies removed. *Some*.
- C4 diagram has been recognised as a requirement for the project.
It will be sorely missed.
- Services still print their own logs to console in container.
This is intentional, as the Seq sink is a busy place.
- Loadbalancing fixed an issue where replicated articleservice services 
would fight for the right to make preliminary checks/migrations to all articledatabases.

### Patch notes october 6th
*I have no swarm but I must loadbalance*

- Loadbalancing er kommet til Zarnath. Det har selvfølgelig rykket docker-compose.yml 
fuldstændigt i stykker, og vi kan ikke længere vente på conditions som "status_healthy",
så det ses gerne at man venter 1-2 minutter fra startup før man benytter app'en.
Om det er rimeligt eller om det her er et unoptimized rod vides ikke før mere data er
indsamlet.
- Projektet startes nu ordenligt med ```docker stack deploy -c docker-compose.yml happyheadlines```
og smadres med ```docker stack rm happyheadlines ```
- Massive ændringer til logging/tracing for at finde ud af, hvorfor de ikke er skrevet 
verbost i seq. Uhensigtsmæssigt som det er, så er den nu registreret i de apps der bruger dem,
samt tilføjet til images. Meget tung løsning, klart forringelse af buildtime.
- Tilføjelse af metoder der kan finde comments til en given article i en given region
- Tilføjelse af shell script til at bygge samtlige dockerfiler i repo.
Kan aktiveres ved at bash ```./DockerBuildAll.sh```
- ```./AddMigrations.sh``` fungerer igen, da hardcoded region er fjernet fra
designtime-venlig dbcontextfactory i articledatabase
- CommentDto tilføjet til ArticleService, men det er noget vås. ignore.
- Tilføjet sleep til ArticleService, så den i hvert fald bliver liggende i 100 sekunder
- Logs virker omkring kl 18 i aften

### Patch notes october 5th
- 1.0 release did not include mandatory cache dashboard. This update does, however functional.
- Several services collided on docker compose up because several appsettings.json files
to publish. This has been fixed by simply ignoring the error and hoping for the best.
- Because of changes made earlier to the MonitorService, 
logs and traces have mysteriously gone missing. The issue is currently under great scrutiny.
- ArticleService previously went down on checking cache. Issue seems to have been
a null-reference in the ConnectionMultiplexer as the used connectionstring was
not present in the appsettings file.
- other miscellaneous changes.