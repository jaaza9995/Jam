using Microsoft.EntityFrameworkCore;
using Jam.Models;
using Jam.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Jam.DAL;

public static class DBInit
{
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<StoryDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // await context.Database.EnsureCreatedAsync();

        // For development only
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // Ensure roles exist
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));

        // Seed users (normal and admin)
        if (!userManager.Users.Any())
        {
            var users = new List<ApplicationUser>
            {
                new() {
                    Firstname = "Barry",
                    Lastname = "Allen",
                    UserName = "TheFlash",
                    Email = "flash@example.com",
                    EmailConfirmed = true
                },
                new() {
                    Firstname = "Emily",
                    Lastname = "Smith",
                    UserName = "Smithy",
                    Email = "emily_smith@gmail.com",
                    EmailConfirmed = true
                },
                new() {
                    Firstname = "Bob",
                    Lastname = "Luthor",
                    UserName = "LexLuthor",
                    Email = "bob@gmail.com",
                    EmailConfirmed = true
                },
                new()
                {
                    Firstname = "Admin",
                    Lastname = "User",
                    UserName = "AdminUser",
                    Email = "admin@jamapp.com",
                    EmailConfirmed = true
                }
            };

            foreach (var user in users)
            {
                string password = "Password123!"; // default for normal users

                if (user.Email == "admin@jamapp.com")
                    password = "AdminPassword123!"; // special password for admin

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign roles
                    if (user.Email == "admin@jamapp.com")
                        await userManager.AddToRoleAsync(user, "Admin");
                    else
                        await userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    Console.WriteLine($"Error creating user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        // (Optional) ensure admin has correct role even if users already existed
        var adminUser = await userManager.FindByEmailAsync("admin@jamapp.com");
        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Seed stories, different scenes, answer options, and playing sessions
        if (!context.Stories.Any())
        {
            var barry = await userManager.FindByEmailAsync("flash@example.com");
            var emily = await userManager.FindByEmailAsync("emily_smith@gmail.com");
            var bob = await userManager.FindByEmailAsync("bob@gmail.com");

            var stories = new List<Story>
            {
                new Story
                {
                    Title = "De tre Bukkene Bruse",
                    Description = "Lille bukk, Mellomste bukk, og Store bukk skal over en bro for å spise gress, men under broen bor et stygt og farlig troll som vil spise dem.",
                    DifficultyLevel = DifficultyLevel.Medium,
                    Accessible = Accessibility.Public,
                    Played = 20,
                    Finished = 12,
                    Failed = 5,
                    Dnf = 3,
                    User = barry,
                },
                new Story
                {
                    Title = "Askeladden som kappot med trollet",
                    Description = "Askeladden, en ung bondegutt, utfordrer et farlig troll til en spisekonkurranse.",
                    DifficultyLevel = DifficultyLevel.Hard,
                    Accessible = Accessibility.Private,
                    Played = 0,
                    Finished = 0,
                    Failed = 0,
                    Dnf = 0,
                    Code = "A123B29",
                    User = emily,
                },
                new Story
                {
                    Title = "Gutten på Sykkelen",
                    Description = "Stian skal en dag sykle til skolen for første gang, men da han skal over det lokale gangfeltet skjer noe uventet!",
                    DifficultyLevel = DifficultyLevel.Easy,
                    Accessible = Accessibility.Public,
                    Played = 0,
                    Finished = 0,
                    Failed = 0,
                    Dnf = 0,
                    User = bob,
                },
            };
            context.Stories.AddRange(stories);

            var intro = new IntroScene
            {
                IntroText = "Det var en gang tre bukkene bruse som skulle til seters for å gjøre seg fete. På veien lå en stor elv, og over den gikk en steinbro. Under broen bodde et troll.",
                Story = stories[0]
            };

            var questionScene1 = new QuestionScene
            {
                SceneText = "Først kom den minste bukken bruse, som trippet over broen. 'Hvem er det som tripper på min bro?' brølte trollet. 'Det er bare lille bukken bruse', sa bukken med sin tynne stemme. 'Jeg skal til seters for å gjøre meg fet.'",
                Question = "Hva er 2 + 2? Svar riktig for å hjelpe lille bukk over broen og vekk fra trollet i trygghet!",
                Story = stories[0]
            };

            var answerOptions1 = new List<AnswerOption>
            {
                new AnswerOption
                {
                    Answer = "4",
                    FeedbackText = "Lille bukk var smart. Han sa: 'Du bør ikke ta meg, vent litt, for etter meg kommer en som er mye større! Trollet lot lille bukk gå...",
                    IsCorrect = true,
                },
                new AnswerOption
                {
                    Answer = "3",
                    FeedbackText = "Å nei du svarte feil! Trollet reiste seg opp fra broen for å spise ham, men lille bukk rakk akkurat å skrike ut: 'IKKE TA MEG! Vent litt, for etter meg kommer en som er mye større! Trollet tvilte, men bestemte seg for å høre på den lille bukken...",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "2",
                    FeedbackText = "Å nei du svarte feil! Trollet ble sint, men lille bukk rakk akkurat å skrike ut: 'IKKE TA MEG! Vent litt, for etter meg kommer en som er mye større! Trollet tvilte, men bestemte seg for å høre på den lille bukken...",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "5",
                    FeedbackText = "Å nei du svarte feil! Trollet begynte å krabbe ut fra broen for å ta lille bukk, men lille bukk rakk akkurat å skrike ut: 'IKKE TA MEG! Vent litt, for etter meg kommer en som er mye større! Trollet tvilte, men bestemte seg for å høre på den lille bukken...",
                    IsCorrect = false,
                },
            };

            questionScene1.AnswerOptions = answerOptions1;

            var questionScene2 = new QuestionScene
            {
                SceneText = "Så kom den mellomste bukken bruse. 'Hvem er det som tramper på min bro?' brølte trollet. 'Det er bare mellomste bukken bruse,' sa han, med sin litt tykkere stemme. 'Jeg skal til seters for å gjøre meg fet, akkurat som lille bukk!",
                Question = "Hva er 4 + 8? Svar riktig for å hjelpe mellomste bukk over broen og vekk fra trollet!",
                Story = stories[0]
            };

            questionScene1.NextQuestionScene = questionScene2;

            var answerOptions2 = new List<AnswerOption>
            {
                new AnswerOption
                {
                    Answer = "12",
                    FeedbackText = "Mellomste bukk var like glup som lille buk. Han sa: 'Dumme troll! Hvorfor skal du ta meg? Han som kommer etter meg er jo mye større! Trollet hørte også på mellomste bukk og lot han gå...",
                    IsCorrect = true,
                },
                new AnswerOption
                {
                    Answer = "10",
                    FeedbackText = "Å nei du svarte feil! Trollet kastet seg ut fra broen og skulle akkurat til å gripe tak i ham. Men den mellomste bukken hoppet unna og ropte: 'NEI! Ikke ta meg! Vent litt, for etter meg kommer en som er enda større, nemlig Store bukk!' Trollet var veldig usikker, men hørte på mellomste bukk...",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "11",
                    FeedbackText = "Å nei du svarte feil! Trollet gikk kjapt ut fra broen og hoppet opp til der mellomste bukk sto. Men mellomste bukk tryglet og ba: 'NEI NEI NEI! ikke ta meg, vær så snill! Vent litt, for etter meg kommer en som er enda større, nemlig Store bukk!' Trollet nølte, men tenkte at han kunne spare kreftene til store bukk og lot mellomste bukk gå...",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "14",
                    FeedbackText = "Å nei du svarte feil! Trollet tok fram en stor stein og skulle akkurat til å kaste den på mellomste bukk, men mellomste bukk rakk akkurat å overtale trollet: 'NEI! Ikke skad meg! Du bør heller vente på bukken etter meg, for han er enda større' Trollet senket armen og krabbet under broen igjen...",
                    IsCorrect = false,
                },
            };

            questionScene2.AnswerOptions = answerOptions2;

            var questionScene3 = new QuestionScene
            {
                SceneText = "Til slutt kom Store bukk, som dundret over broen. 'Hvem er det som dundrer på min bro?' brølte trollet, som var sint og sulten. Denne gangen var ikke bukken redd. Han sa med sin dype stemme: 'DET ER STORE BUKKEN BRUSE!",
                Question = "Hva er 3 - 5? Svar riktig for å hjelpe store bukk over broen!",
                Story = stories[0]
            };

            questionScene2.NextQuestionScene = questionScene3;

            var answerOptions3 = new List<AnswerOption>
            {
                new AnswerOption
                {
                    Answer = "-2",
                    FeedbackText = "Store bukk var ikke som de andre. Han var modig og sterk, og han hadde ingen planer om å la seg spise. Han svarte bestemt: 'JEG KOMMER FOR Å STANGE DEG!'",
                    IsCorrect = true,
                },
                new AnswerOption
                {
                    Answer = "0",
                    FeedbackText = "Å nei du svarte feil! Nå hadde trollet fått nok! Han stormet ut fra broen, hoppet opp og kastet seg over store bukk!",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "-3",
                    FeedbackText = "Å nei du svarte feil! Nå var trollet illsint! Han hoppet opp og kastet seg over store bukk!",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "2",
                    FeedbackText = "Å nei du svarte feil! Nå var trollet så lei av lille og mellomste bukk, nå skulle han endelig ta store bukk. Han hoppet opp og kastet seg over store bukk!",
                    IsCorrect = false,
                },
            };

            questionScene3.AnswerOptions = answerOptions3;

            var questionScene4 = new QuestionScene
            {
                SceneText = "De kjempet lenge. Store bukk stanget til trollet med sine horn, mens trollet reiv av pelsen til store bukk! Hvordan ender dette?",
                Question = "Hva er 2 + 2 - 3 + 5? Svar riktig for å hjelpe store bukk i kampen mot trollet!",
                Story = stories[0]
            };

            questionScene3.NextQuestionScene = questionScene4;

            var answerOptions4 = new List<AnswerOption>
            {
                new AnswerOption
                {
                    Answer = "6",
                    FeedbackText = "YES! Du svarte riktig! Store bukk fikk sveivet hornerne rett inn i magen på trollet! 'AAAUUUUU!!!', skrek trollet.",
                    IsCorrect = true,
                },
                new AnswerOption
                {
                    Answer = "4",
                    FeedbackText = "Å nei du svarte feil! Trollet slo så hardt til store bukk at han nærmest mistet synet!!",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "8",
                    FeedbackText = "Å nei du svarte feil! Trollet løfter opp store bukk og kastet han hardt ned i steinbroen!",
                    IsCorrect = false,
                },
                new AnswerOption
                {
                    Answer = "-2",
                    FeedbackText = "Å nei du svarte feil! Trollet kastet store bukk hardt inn i rekkverket på broen!",
                    IsCorrect = false,
                },
            };

            questionScene4.AnswerOptions = answerOptions4;

            var goodEnding = new EndingScene
            {
                EndingType = EndingType.Good,
                EndingText = "Til tross for en spennedne kamp, klarte til slutt store bukk å stange trollet ned fra broen. Trollet falt ut i elva og slo seg skikkelig. Alle tre bukkene kom seg trygt over broen. De spiste og koste seg, og levde et lykkelig liv. De ble aldri mer plaget av trollet...",
                Story = stories[0]
            };

            var neutralEnding = new EndingScene
            {
                EndingType = EndingType.Neutral,
                EndingText = "Etter en lang kamp, så falt både Store bukk og trollet ut fra broen og skadet seg skikkelig. Store bukk var litt lettere en trollet og kom seg om sider opp og ut av elva. Trollet derimot ble tatt videre med elva. De tre bukkene fikk spist og ble mette, men så seg alltid over skuldrene for å vokte seg for trollet...",
                Story = stories[0]
            };

            var badEnding = new EndingScene
            {
                EndingType = EndingType.Bad,
                EndingText = "Store bukk klarte ikke å stange trollet ut i elva, og trollet spiste ham. Lille bukk og mellomste bukk levde i sorg, og turte aldri å gå tilbake over broen igjen. De måtte finne seg et nytt sted å spise.",
                Story = stories[0]
            };

            context.IntroScenes.Add(intro);
            context.QuestionScenes.AddRange(questionScene1, questionScene2, questionScene3, questionScene4);
            context.EndingScenes.AddRange(goodEnding, neutralEnding, badEnding);
            context.AnswerOptions.AddRange(answerOptions1.Concat(answerOptions2).Concat(answerOptions3).Concat(answerOptions4));
            await context.SaveChangesAsync();

            var theFlashPlayingSession = new PlayingSession
            {
                StartTime = DateTime.Now.AddMinutes(-8),
                EndTime = DateTime.Now,
                Score = 25,
                MaxScore = 40,
                CurrentLevel = 3,
                CurrentSceneId = neutralEnding.EndingSceneId,
                CurrentSceneType = SceneType.Ending,
                Story = stories[1],
                User = barry,
            };

            var smithyPlayingSession = new PlayingSession
            {
                StartTime = DateTime.Now.AddMinutes(-15),
                Score = 10,
                MaxScore = 40,
                CurrentLevel = 3,
                CurrentSceneId = questionScene2.QuestionSceneId,
                CurrentSceneType = SceneType.Question,
                Story = stories[0],
                User = emily,
            };

            context.PlayingSessions.AddRange(theFlashPlayingSession, smithyPlayingSession);
            await context.SaveChangesAsync();
        }
    }
}