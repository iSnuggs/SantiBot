#nullable disable
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Fun")]
    [Group("fun")]
    public partial class FunCommands : SantiModule
    {
        private static readonly SantiRandom _rng = new();

        // ────────────────────────────────────────────
        // 1. EightBall — 20+ magic 8-ball responses
        // ────────────────────────────────────────────

        private static readonly string[] _eightBallResponses =
        [
            "It is certain.",
            "It is decidedly so.",
            "Without a doubt.",
            "Yes, definitely.",
            "You may rely on it.",
            "As I see it, yes.",
            "Most likely.",
            "Outlook good.",
            "Yes.",
            "Signs point to yes.",
            "Reply hazy, try again.",
            "Ask again later.",
            "Better not tell you now.",
            "Cannot predict now.",
            "Concentrate and ask again.",
            "Don't count on it.",
            "My reply is no.",
            "My sources say no.",
            "Outlook not so good.",
            "Very doubtful.",
            "Absolutely not. Next question.",
            "The stars say... maybe after lunch.",
            "I asked my cat. She walked away. So... no.",
            "Bold of you to assume I know things.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task EightBall([Leftover] string question = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            var response = _eightBallResponses[_rng.Next(0, _eightBallResponses.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🎱 Magic 8-Ball")
                         .AddField("❓ Question", question)
                         .AddField("🎱 Answer", response)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 2. Tarot — 22 Major Arcana cards
        // ────────────────────────────────────────────

        private static readonly (string Card, string Meaning)[] _tarotCards =
        [
            ("0 — The Fool", "New beginnings, spontaneity, and a free spirit. Take a leap of faith."),
            ("I — The Magician", "Willpower, resourcefulness, and skill. You have everything you need."),
            ("II — The High Priestess", "Intuition, mystery, and inner knowledge. Trust your gut feelings."),
            ("III — The Empress", "Abundance, nurturing, and fertility. Creativity is flowing through you."),
            ("IV — The Emperor", "Authority, structure, and stability. Take charge of your situation."),
            ("V — The Hierophant", "Tradition, conformity, and spiritual wisdom. Seek guidance from a mentor."),
            ("VI — The Lovers", "Love, harmony, and choices. A meaningful relationship or decision awaits."),
            ("VII — The Chariot", "Determination, willpower, and triumph. Victory is within your grasp."),
            ("VIII — Strength", "Courage, patience, and inner strength. You are braver than you think."),
            ("IX — The Hermit", "Soul-searching, introspection, and solitude. Look within for answers."),
            ("X — Wheel of Fortune", "Change, cycles, and destiny. The wheel is turning in your favor."),
            ("XI — Justice", "Fairness, truth, and law. What you put out will come back to you."),
            ("XII — The Hanged Man", "Surrender, letting go, and new perspectives. Pause and reflect."),
            ("XIII — Death", "Endings, transformation, and transition. Something must end for new things to begin."),
            ("XIV — Temperance", "Balance, moderation, and patience. Find your middle ground."),
            ("XV — The Devil", "Bondage, materialism, and shadow self. Break free from what holds you back."),
            ("XVI — The Tower", "Sudden upheaval, revelation, and awakening. Chaos brings clarity."),
            ("XVII — The Star", "Hope, inspiration, and serenity. Brighter days are ahead."),
            ("XVIII — The Moon", "Illusion, fear, and the subconscious. Not everything is as it seems."),
            ("XIX — The Sun", "Joy, success, and vitality. Happiness and warmth surround you."),
            ("XX — Judgement", "Reflection, reckoning, and inner calling. Rise up and embrace your purpose."),
            ("XXI — The World", "Completion, accomplishment, and fulfillment. You have come full circle."),
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Tarot()
        {
            var (card, meaning) = _tarotCards[_rng.Next(0, _tarotCards.Length)];
            var reversed = _rng.Next(0, 2) == 0;

            var desc = reversed
                ? $"**{card}** *(Reversed)*\n\n{meaning}\n\n_Reversed: The energy of this card is blocked or internalized. Reflect on what might be holding you back._"
                : $"**{card}** *(Upright)*\n\n{meaning}";

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🔮 Tarot Reading")
                         .WithDescription(desc)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 3. Fortune — 50+ fortune cookie messages
        // ────────────────────────────────────────────

        private static readonly string[] _fortunes =
        [
            "A beautiful, smart, and loving person will come into your life. Right after you click this again.",
            "A dubious friend may be an enemy in camouflage. Or just bad at laundry.",
            "A fresh start will put you on your way. But first, coffee.",
            "A golden egg of opportunity falls into your lap this month.",
            "A lifetime of happiness lies ahead of you. Terms and conditions may apply.",
            "A pleasant surprise is waiting for you. Check your pockets.",
            "A short pencil is usually better than a long memory any day.",
            "A smile is your personal welcome mat. Also free.",
            "Accept something you cannot change, and you will feel better. Like gravity.",
            "All your hard work will soon pay off. The 'soon' is relative.",
            "An important person will offer you support. Accept it graciously.",
            "Be on the alert to recognize your prime at whatever time of your life it may occur.",
            "Believe in yourself and others will too. Fake it till you make it.",
            "Better to be the hammer than the nail. Unless you're hanging art.",
            "Change is happening in your life, so go with the flow. Resistance is futile.",
            "Courtesy is contagious. Spread it like confetti.",
            "Curiosity kills boredom. Nothing can kill curiosity.",
            "Dedicate yourself with a calm mind to the task at hand. Then panic later.",
            "Determination is what you need now. And maybe a snack.",
            "Disbelief destroys the magic. Just roll with it.",
            "Do not be afraid of competition. Also, do not be afraid of snacks.",
            "Do not let ambitions overshadow small success. Small wins add up.",
            "Do not make extra work for yourself. Unless it's fun.",
            "Don't just think. Act. Then think about what you acted on.",
            "Don't worry about money. The best things in life are free. Except rent.",
            "Every flower blooms in its own time. You're a late bloomer, and that's okay.",
            "Every friend started off as a stranger. Think about that.",
            "Failure is the chance to do better next time. Fortune cookie said so.",
            "Fear is just excitement without breath. Breathe.",
            "Follow your heart and you will be rewarded. GPS optional.",
            "Good news will come from far away. Check your spam folder.",
            "Hard work pays off in the future. Laziness pays off now.",
            "If you continually give, you will continually have.",
            "In order to take, one must first give. That's just physics. Probably.",
            "It is better to deal with problems before they arise. But where's the fun?",
            "It's time to get moving. Your next adventure awaits around the corner.",
            "Keep your eyes open. Opportunity may present itself in unexpected ways.",
            "Land is always on the mind of a flying bird. Deep, right?",
            "Love is the only thing that can be divided without being diminished.",
            "Miles are covered one step at a time. Unless you have a car.",
            "Nature, time, and patience are the three great physicians.",
            "Now is the time to try something new. Like reading another fortune.",
            "Opportunity knocks on your door every day. Answer it in something nicer than pajamas.",
            "People learn little from success, but much from failure. Congrats on all the learning.",
            "Practice makes perfect. But nobody's perfect. So why practice? Just kidding. Practice.",
            "Remember, today is the tomorrow you worried about yesterday.",
            "Smile. Tomorrow is another day to make mistakes and blame them on Mercury retrograde.",
            "The early bird gets the worm, but the second mouse gets the cheese.",
            "The one that is worth having is worth waiting for.",
            "The road to success is always under construction. Wear a hard hat.",
            "You are about to receive a big compliment. From yourself. In the mirror.",
            "You will have a very pleasant experience. Probably involving food.",
            "Your creativity will take you places you never imagined. Pack a bag.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fortune()
        {
            var fortune = _fortunes[_rng.Next(0, _fortunes.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🥠 Fortune Cookie")
                         .WithDescription(fortune)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 4. Joke — 50+ jokes
        // ────────────────────────────────────────────

        private static readonly string[] _jokes =
        [
            "Why don't scientists trust atoms? Because they make up everything.",
            "I told my wife she was drawing her eyebrows too high. She looked surprised.",
            "What do you call a fake noodle? An impasta.",
            "Why did the scarecrow win an award? He was outstanding in his field.",
            "I'm reading a book about anti-gravity. It's impossible to put down.",
            "Did you hear about the mathematician who's afraid of negative numbers? He'll stop at nothing to avoid them.",
            "Why don't skeletons fight each other? They don't have the guts.",
            "What do you call cheese that isn't yours? Nacho cheese.",
            "Why couldn't the bicycle stand up by itself? It was two-tired.",
            "What did the ocean say to the beach? Nothing, it just waved.",
            "Why do chicken coops only have two doors? Because if they had four, they'd be chicken sedans.",
            "I used to hate facial hair, but then it grew on me.",
            "What do you call a bear with no teeth? A gummy bear.",
            "I told a chemistry joke. There was no reaction.",
            "Why did the math book look so sad? Because it had too many problems.",
            "What's the best thing about Switzerland? I don't know, but the flag is a big plus.",
            "I used to play piano by ear, but now I use my hands.",
            "What did the grape do when it got stepped on? Nothing, it just let out a little wine.",
            "Why do seagulls fly over the ocean? Because if they flew over the bay, they'd be bagels.",
            "I'm on a seafood diet. I see food and I eat it.",
            "How does a penguin build its house? Igloos it together.",
            "What do you call a dog that does magic tricks? A Labracadabrador.",
            "Why don't eggs tell jokes? They'd crack each other up.",
            "What did one wall say to the other wall? I'll meet you at the corner.",
            "What do you get when you cross a snowman and a vampire? Frostbite.",
            "Why was the math book always stressed? It had too many problems and no solutions.",
            "What's brown and sticky? A stick.",
            "How do you organize a space party? You planet.",
            "Why did the golfer bring two pairs of pants? In case he got a hole in one.",
            "What do you call a sleeping dinosaur? A dino-snore.",
            "I would tell you a construction joke, but I'm still working on it.",
            "What do dentists call their X-rays? Tooth pics.",
            "Why did the coffee file a police report? It got mugged.",
            "What do you call a lazy kangaroo? A pouch potato.",
            "Why don't oysters share? Because they're shellfish.",
            "How do trees access the internet? They log in.",
            "What kind of music do mummies listen to? Wrap music.",
            "Why did the stadium get hot after the game? All the fans left.",
            "What do you call a factory that makes okay products? A satisfactory.",
            "Why do bees have sticky hair? Because they use honeycombs.",
            "What did the fish say when it hit the wall? Dam.",
            "Why can't your nose be twelve inches long? Because then it would be a foot.",
            "I asked the librarian if they had any books on paranoia. She whispered, 'They're right behind you.'",
            "What do you call a cow with no legs? Ground beef.",
            "What's orange and sounds like a parrot? A carrot.",
            "Why did the tomato turn red? Because it saw the salad dressing.",
            "What do you call an alligator in a vest? An investigator.",
            "How do you make a tissue dance? Put a little boogie in it.",
            "What did the janitor say when he jumped out of the closet? Supplies!",
            "Why don't scientists trust stairs? Because they're always up to something.",
            "What sits at the bottom of the sea and twitches? A nervous wreck.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Joke()
        {
            var joke = _jokes[_rng.Next(0, _jokes.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("😂 Random Joke")
                         .WithDescription(joke)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 5. Fact — 50+ random fun facts
        // ────────────────────────────────────────────

        private static readonly string[] _facts =
        [
            "Honey never spoils. Archaeologists have found 3,000-year-old honey in Egyptian tombs that was still edible.",
            "Octopuses have three hearts, nine brains, and blue blood.",
            "A group of flamingos is called a 'flamboyance.'",
            "Bananas are berries, but strawberries aren't.",
            "The shortest war in history was between Britain and Zanzibar in 1896. It lasted 38 minutes.",
            "A jiffy is an actual unit of time: one trillionth of a second.",
            "The inventor of the Pringles can is buried in one.",
            "There are more possible iterations of a game of chess than there are atoms in the observable universe.",
            "Cows have best friends and get stressed when separated.",
            "The tongue of a blue whale weighs as much as an elephant.",
            "Venus is the only planet that spins clockwise.",
            "A bolt of lightning is five times hotter than the surface of the sun.",
            "Oxford University is older than the Aztec Empire.",
            "Cleopatra lived closer in time to the moon landing than to the construction of the Great Pyramid.",
            "There are more trees on Earth than stars in the Milky Way.",
            "The total weight of all ants on Earth is roughly the same as the total weight of all humans.",
            "Wombat poop is cube-shaped to stop it from rolling away.",
            "Scotland's national animal is the unicorn.",
            "The heart of a shrimp is located in its head.",
            "Dolphins have names for each other and can call out to specific individuals.",
            "A single strand of spaghetti is called a spaghetto.",
            "Sea otters hold hands while sleeping so they don't drift apart.",
            "The Eiffel Tower can grow up to 6 inches taller during the summer due to heat expansion.",
            "A day on Venus is longer than a year on Venus.",
            "Sloths can hold their breath longer than dolphins — up to 40 minutes.",
            "The world's oldest known living tree is over 5,000 years old.",
            "Humans share about 60% of their DNA with bananas.",
            "There's a basketball court on the top floor of the U.S. Supreme Court building.",
            "Butterflies taste with their feet.",
            "The moon has moonquakes, just like Earth has earthquakes.",
            "A cloud can weigh more than a million pounds.",
            "The longest hiccuping spree lasted 68 years.",
            "Crows can recognize human faces and hold grudges.",
            "Your stomach gets a new lining every three to four days to prevent it from digesting itself.",
            "The first oranges weren't orange — they were green.",
            "A group of porcupines is called a prickle.",
            "Hot water freezes faster than cold water, and nobody fully knows why. It's called the Mpemba effect.",
            "The average person walks the equivalent of three times around the world in a lifetime.",
            "Sharks are older than trees. Sharks have been around for about 400 million years.",
            "There are more fake flamingos in the world than real ones.",
            "Nintendo was founded in 1889 as a playing card company.",
            "A photon takes about 8 minutes to travel from the Sun to Earth, but 100,000 years to travel from the Sun's core to its surface.",
            "Astronauts grow up to 2 inches taller in space.",
            "The longest English word without a vowel is 'rhythms.'",
            "The smell of freshly cut grass is actually a plant distress call.",
            "An octopus has a donut-shaped brain, and its esophagus runs through it.",
            "You can't hum while holding your nose. You just tried, didn't you?",
            "The inventor of the fire hydrant is unknown because the patent was destroyed in a fire.",
            "A flock of crows is known as a murder.",
            "The average human body contains enough iron to make a nail about 3 inches long.",
            "Polar bear fur is actually transparent, not white.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fact()
        {
            var fact = _facts[_rng.Next(0, _facts.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🧠 Random Fun Fact")
                         .WithDescription(fact)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 6. Compliment — 30+ compliments
        // ────────────────────────────────────────────

        private static readonly string[] _compliments =
        [
            "You're the reason the gene pool needs a lifeguard — too much awesome.",
            "If you were a vegetable, you'd be a cute-cumber.",
            "You have the best laugh. It's basically free therapy for everyone around you.",
            "You're like a human sunrise. People just feel better when you show up.",
            "Your energy is more refreshing than a cold drink on a hot day.",
            "You could make a grumpy cat smile.",
            "If kindness were currency, you'd be a billionaire.",
            "You bring the fun to every party without even trying.",
            "You're proof that good things come in awesome packages.",
            "Your smile is basically a superpower.",
            "You make awkward silences comfortable somehow.",
            "You're the type of person everyone wants on their team.",
            "The world is better with you in it. That's just math.",
            "You have the emotional range of a Pixar movie — deep and delightful.",
            "Your vibe is immaculate. Truly unmatched.",
            "You make difficult things look easy and easy things look fun.",
            "If there were an Olympic event for being awesome, you'd get gold every time.",
            "Your personality could outshine a disco ball.",
            "You have the kind of confidence that makes everyone around you feel confident too.",
            "You're like Wi-Fi — people are happier when you're connected.",
            "You could charm the socks off a snake. And snakes don't even wear socks.",
            "You're the kind of person people write songs about.",
            "Your brain is as impressive as your personality, and that's saying something.",
            "You radiate good energy like a walking power plant of positivity.",
            "If being cool were a job, you'd be CEO.",
            "You're like a four-leaf clover — rare and lucky to find.",
            "Your creativity could put artists to shame.",
            "You have an aura that makes everyone want to be your friend.",
            "You make Monday mornings feel like Friday evenings.",
            "You're not just a snack. You're the whole meal deal.",
            "Your kindness is the kind they should teach in schools.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Compliment(IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;
            var compliment = _compliments[_rng.Next(0, _compliments.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("💖 Compliment")
                         .WithDescription($"{user.Mention}, {compliment}")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 7. Insult — 30+ playful/funny insults
        // ────────────────────────────────────────────

        private static readonly string[] _insults =
        [
            "You bring everyone so much joy... when you leave the room.",
            "You're like a cloud. Everything brightens up when you disappear.",
            "You're the human equivalent of a participation trophy.",
            "If you were a spice, you'd be flour.",
            "You're not the dumbest person in the world, but you'd better hope they don't die.",
            "I'd explain it to you, but I left my crayons at home.",
            "You're proof that even evolution makes mistakes sometimes.",
            "You're the reason they put instructions on shampoo bottles.",
            "You're about as useful as the 'ueue' in 'queue.'",
            "You have the survival instincts of a lemming with a death wish.",
            "You're like a software update — when I see you, I think 'not now.'",
            "If brains were dynamite, you wouldn't have enough to blow your nose.",
            "You're the WiFi signal that shows full bars but loads nothing.",
            "You're the kind of person who would lose a debate with Siri.",
            "If you were any more basic, you'd be a tutorial level.",
            "You bring a lot to the table. Unfortunately, it's all dishes.",
            "Somewhere out there, a tree is tirelessly producing oxygen for you. You owe it an apology.",
            "You're like a screen door on a submarine — not super helpful.",
            "You have the attention span of a goldfish on espresso.",
            "You're the human equivalent of a 'Terms and Conditions' page. Everyone just skips past you.",
            "You're not completely useless. You can always serve as a bad example.",
            "You're like a penny — two-faced and not worth much.",
            "If ignorance is bliss, you must be the happiest person alive.",
            "You're the reason God created the middle finger.",
            "I'd challenge you to a battle of wits, but I see you came unarmed.",
            "You're about as sharp as a bowling ball.",
            "You couldn't pour water out of a boot if the instructions were on the heel.",
            "You're like a broken pencil — pointless.",
            "If common sense were common, you'd still be the exception.",
            "You have the personality of a damp towel.",
            "You're living proof that nature has a sense of humor.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Insult(IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;
            var insult = _insults[_rng.Next(0, _insults.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🔥 Playful Insult")
                         .WithDescription($"{user.Mention}, {insult}")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 8. PickupLine — 30+ pickup lines
        // ────────────────────────────────────────────

        private static readonly string[] _pickupLines =
        [
            "Are you a magician? Because whenever I look at you, everyone else disappears.",
            "Do you have a map? Because I just got lost in your eyes.",
            "Are you a campfire? Because you're hot and I want s'more.",
            "Is your name Google? Because you have everything I've been searching for.",
            "Do you have a Band-Aid? Because I just scraped my knee falling for you.",
            "Are you a parking ticket? Because you've got 'fine' written all over you.",
            "Is your dad a boxer? Because you're a knockout.",
            "Do you believe in love at first sight, or should I walk by again?",
            "Are you a time traveler? Because I can see you in my future.",
            "If you were a vegetable, you'd be a cute-cumber.",
            "Are you Wi-Fi? Because I'm feeling a connection.",
            "Is your name Chapstick? Because you're da balm.",
            "Are you a bank loan? Because you've got my interest.",
            "Do you have a sunburn, or are you always this hot?",
            "If beauty were time, you'd be an eternity.",
            "Are you a 45-degree angle? Because you're acute-y.",
            "Is this the Hogwarts Express? Because it feels like you and I are headed somewhere magical.",
            "Are you a keyboard? Because you're just my type.",
            "Do you have a mirror in your pocket? Because I can see myself in your pants. Wait—",
            "Are you a volcano? Because I lava you.",
            "If you were a triangle, you'd be acute one.",
            "Are you made of copper and tellurium? Because you're Cu-Te.",
            "I must be a snowflake, because I've fallen for you.",
            "Are you a cat? Because you're purr-fect.",
            "Is your name Ariel? Because I think we mermaid for each other.",
            "I'm not a photographer, but I can picture us together.",
            "Are you a dictionary? Because you add meaning to my life.",
            "If kisses were snowflakes, I'd send you a blizzard.",
            "You must be a broom, because you just swept me off my feet.",
            "Are you an alien? Because you just abducted my heart.",
            "Do you have a quarter? Because I want to call my mom and tell her I met the one.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PickupLine()
        {
            var line = _pickupLines[_rng.Next(0, _pickupLines.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("💘 Pickup Line")
                         .WithDescription(line)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 9. HotTake — 40+ hot takes
        // ────────────────────────────────────────────

        private static readonly string[] _hotTakes =
        [
            "Cereal is soup.",
            "Hot dogs are tacos.",
            "Water is the best beverage ever invented. Fight me.",
            "Pineapple absolutely belongs on pizza.",
            "The best part of the Oreo is the cookie, not the cream.",
            "Breakfast for dinner is better than breakfast for breakfast.",
            "Socks with sandals is a power move.",
            "Movie theaters should have intermissions.",
            "Raisins are good in cookies. There, I said it.",
            "The toilet paper should go UNDER, not over.",
            "GIF is pronounced 'jif' and I will die on this hill.",
            "Sparkling water tastes like TV static.",
            "Ketchup on scrambled eggs is valid.",
            "Sleeping with socks on is perfectly fine.",
            "The book is NOT always better than the movie.",
            "Cold pizza is better than reheated pizza.",
            "Chocolate ice cream is overrated.",
            "Cats are better than dogs. (Don't @ me.)",
            "Adulting is just Googling how to do stuff.",
            "Rainy days are better than sunny days.",
            "The middle seat on a plane gets BOTH armrests. It's only fair.",
            "Crocs are fashionable footwear.",
            "Vanilla is a more complex flavor than chocolate.",
            "People who back into parking spots are superior drivers.",
            "You should put milk in the bowl before cereal. Hear me out.",
            "Wrapping presents is a waste of time. Use gift bags.",
            "Summer is the worst season. It's just sweating with extra steps.",
            "Ranch dressing goes with everything.",
            "People who say 'I don't watch TV' are not more interesting.",
            "Candy corn is delicious and you're all lying.",
            "Birds are just government drones with extra steps.",
            "Putting ice in milk makes it better.",
            "Microwaved leftover pizza is a war crime.",
            "The Oxford comma is not optional. It's essential.",
            "Pancakes are better than waffles. Waffles are just pancakes with abs.",
            "Mint chocolate chip ice cream tastes like toothpaste. Good toothpaste, but still.",
            "Mayo is the superior condiment.",
            "New Year's Eve is the most overrated holiday.",
            "People who recline their airplane seats are living their best life.",
            "A hot dog is a sandwich. Deal with it.",
            "Nutella is just fancy chocolate frosting.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HotTake()
        {
            var take = _hotTakes[_rng.Next(0, _hotTakes.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🌶️ Hot Take")
                         .WithDescription(take)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 10. WouldYouRather — 40+ questions
        // ────────────────────────────────────────────

        private static readonly string[] _wouldYouRather =
        [
            "Would you rather be able to fly or be invisible?",
            "Would you rather have unlimited money or unlimited time?",
            "Would you rather fight one horse-sized duck or a hundred duck-sized horses?",
            "Would you rather always be 10 minutes late or always be 20 minutes early?",
            "Would you rather have the ability to read minds or see the future?",
            "Would you rather live without music or without TV?",
            "Would you rather have a rewind button or a pause button for your life?",
            "Would you rather be able to talk to animals or speak every human language?",
            "Would you rather have unlimited pizza or unlimited tacos for life?",
            "Would you rather be the funniest person in the room or the smartest?",
            "Would you rather have no internet or no air conditioning for the rest of your life?",
            "Would you rather be famous but hated or unknown but loved?",
            "Would you rather never be able to use social media again or never watch another movie?",
            "Would you rather be stuck in an elevator with your ex or your boss?",
            "Would you rather have a personal chef or a personal driver?",
            "Would you rather always say everything on your mind or never speak again?",
            "Would you rather live in the Harry Potter universe or the Marvel universe?",
            "Would you rather have to sing everything you say or dance everywhere you go?",
            "Would you rather be Batman or Iron Man?",
            "Would you rather have the power of super speed or super strength?",
            "Would you rather eat only sweet or only savory food for the rest of your life?",
            "Would you rather know the date of your death or the cause of your death?",
            "Would you rather have a house with no windows or no doors?",
            "Would you rather never eat cheese again or never eat chocolate again?",
            "Would you rather relive the same day forever or fast-forward 10 years?",
            "Would you rather be a wizard in Harry Potter or a Jedi in Star Wars?",
            "Would you rather have free Wi-Fi everywhere or free coffee everywhere?",
            "Would you rather give up video games or give up movies?",
            "Would you rather always have to whisper or always have to shout?",
            "Would you rather live in a treehouse or a houseboat?",
            "Would you rather have a pet dragon or a pet unicorn?",
            "Would you rather be able to control fire or water?",
            "Would you rather be the best player on a bad team or the worst player on a great team?",
            "Would you rather have no taste or no smell?",
            "Would you rather age only from the neck up or only from the neck down?",
            "Would you rather live in a world without seasons or a world without weather changes?",
            "Would you rather be able to teleport or stop time?",
            "Would you rather have your dream job but low pay or a boring job with amazing pay?",
            "Would you rather have a photographic memory or an IQ of 200?",
            "Would you rather always be overdressed or always be underdressed?",
            "Would you rather live without heating or without cooling?",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task WouldYouRather()
        {
            var question = _wouldYouRather[_rng.Next(0, _wouldYouRather.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🤔 Would You Rather")
                         .WithDescription(question)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 11. ThisOrThat — 30+ questions
        // ────────────────────────────────────────────

        private static readonly string[] _thisOrThat =
        [
            "🅰️ Netflix **or** 🅱️ YouTube?",
            "🅰️ Morning person **or** 🅱️ Night owl?",
            "🅰️ Dogs **or** 🅱️ Cats?",
            "🅰️ Summer **or** 🅱️ Winter?",
            "🅰️ Coffee **or** 🅱️ Tea?",
            "🅰️ Books **or** 🅱️ Movies?",
            "🅰️ Pizza **or** 🅱️ Burgers?",
            "🅰️ Beach **or** 🅱️ Mountains?",
            "🅰️ Sweet **or** 🅱️ Salty?",
            "🅰️ Marvel **or** 🅱️ DC?",
            "🅰️ iPhone **or** 🅱️ Android?",
            "🅰️ Money **or** 🅱️ Fame?",
            "🅰️ Invisibility **or** 🅱️ Flight?",
            "🅰️ Past **or** 🅱️ Future?",
            "🅰️ Pancakes **or** 🅱️ Waffles?",
            "🅰️ Rain **or** 🅱️ Snow?",
            "🅰️ Spotify **or** 🅱️ Apple Music?",
            "🅰️ Texting **or** 🅱️ Calling?",
            "🅰️ Ninjas **or** 🅱️ Pirates?",
            "🅰️ Ice cream **or** 🅱️ Cake?",
            "🅰️ City **or** 🅱️ Countryside?",
            "🅰️ Board games **or** 🅱️ Video games?",
            "🅰️ Pen **or** 🅱️ Pencil?",
            "🅰️ Hot food **or** 🅱️ Cold food?",
            "🅰️ Star Wars **or** 🅱️ Star Trek?",
            "🅰️ Save money **or** 🅱️ Spend money?",
            "🅰️ Comedy **or** 🅱️ Horror?",
            "🅰️ Roller coasters **or** 🅱️ Water slides?",
            "🅰️ Sunrise **or** 🅱️ Sunset?",
            "🅰️ Tacos **or** 🅱️ Burritos?",
            "🅰️ Sneakers **or** 🅱️ Boots?",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ThisOrThat()
        {
            var question = _thisOrThat[_rng.Next(0, _thisOrThat.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("⚖️ This or That")
                         .WithDescription(question)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 12. TruthOrDare — 30 truths + 30 dares
        // ────────────────────────────────────────────

        private static readonly string[] _truths =
        [
            "What is the most embarrassing thing you've ever done in public?",
            "What's the biggest lie you've ever told?",
            "What's your most irrational fear?",
            "Have you ever pretended to like a gift you actually hated?",
            "What's the longest you've gone without showering?",
            "What's the weirdest thing you've ever eaten?",
            "Have you ever snooped through someone else's phone?",
            "What is the most childish thing you still do?",
            "What's a secret you've never told anyone?",
            "Have you ever blamed someone else for something you did?",
            "What's the dumbest thing you've ever cried about?",
            "What's the most useless talent you have?",
            "Have you ever had a crush on a fictional character?",
            "What's the worst haircut you've ever gotten?",
            "What's the most embarrassing song on your playlist?",
            "Have you ever walked into a glass door?",
            "What's the most trouble you've ever gotten into?",
            "Have you ever pretended to be sick to skip something?",
            "What's the most ridiculous thing you've believed as a kid?",
            "What's the worst date you've ever been on?",
            "Have you ever re-gifted a present?",
            "What's the pettiest thing you've ever done?",
            "What's the most embarrassing thing in your search history?",
            "Have you ever talked to yourself in public?",
            "What is your guilty pleasure that you're ashamed of?",
            "What's the last lie you told?",
            "Have you ever had a wardrobe malfunction in public?",
            "What's the most money you've wasted on something stupid?",
            "Have you ever pretended to understand something when you had no idea?",
            "What's the weirdest dream you've ever had?",
        ];

        private static readonly string[] _dares =
        [
            "Send a message in this channel using only emojis for the next 5 minutes.",
            "Change your Discord nickname to something embarrassing for 10 minutes.",
            "Type a message with your eyes closed right now.",
            "Compliment every person who sends a message in the next 2 minutes.",
            "Talk in the third person for the next 5 messages.",
            "Share the last photo you took on your phone (describe it if you can't share).",
            "Use a random accent for your next 3 voice messages.",
            "Send a pickup line to the last person who messaged in this channel.",
            "Only type in CAPS LOCK for the next 3 minutes.",
            "Share your most-used emoji and explain why.",
            "React to the next 5 messages with the most random emoji you can find.",
            "Describe your current outfit in extreme detail.",
            "Send a voice message singing the first song that comes to mind.",
            "Let someone else send a message from your account.",
            "Post your screen time for today.",
            "Only respond with GIFs for the next 5 minutes.",
            "Type everything backwards for your next 3 messages.",
            "Share the last thing you Googled.",
            "Give your honest first impression of the person above you.",
            "Write a haiku about the last thing you ate.",
            "Rate yourself out of 10 and explain your rating.",
            "Tell us your most unpopular opinion.",
            "Do 10 pushups and report back (honor system).",
            "Describe your day using only movie titles.",
            "Share the oldest photo in your camera roll (describe it).",
            "Talk like a pirate for the next 5 messages.",
            "Say something nice about everyone currently online.",
            "Share a fun fact about yourself nobody here knows.",
            "Use no vowels in your next message (try your best).",
            "Let the group pick your profile picture for 24 hours.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TruthOrDare()
        {
            var isTruth = _rng.Next(0, 2) == 0;
            string prompt;
            string title;

            if (isTruth)
            {
                prompt = _truths[_rng.Next(0, _truths.Length)];
                title = "🔍 Truth";
            }
            else
            {
                prompt = _dares[_rng.Next(0, _dares.Length)];
                title = "🎯 Dare";
            }

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle($"🎲 Truth or Dare — {title}")
                         .WithDescription(prompt)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 13. NeverHaveIEver — 30+ prompts
        // ────────────────────────────────────────────

        private static readonly string[] _neverHaveIEver =
        [
            "Never have I ever forgotten someone's name right after they told me.",
            "Never have I ever stalked someone's social media so deep I accidentally liked an old post.",
            "Never have I ever pretended to text to avoid talking to someone.",
            "Never have I ever eaten food off the floor.",
            "Never have I ever blamed a fart on someone else.",
            "Never have I ever waved back at someone who wasn't waving at me.",
            "Never have I ever Googled myself.",
            "Never have I ever sent a text to the wrong person.",
            "Never have I ever walked into the wrong bathroom.",
            "Never have I ever laughed at something I didn't understand just to fit in.",
            "Never have I ever binge-watched an entire season in one sitting.",
            "Never have I ever pretended to be on a phone call to avoid someone.",
            "Never have I ever cried during a movie or TV show.",
            "Never have I ever tripped in public and pretended I was jogging.",
            "Never have I ever eaten something that was meant for someone else.",
            "Never have I ever re-read a text before sending it more than five times.",
            "Never have I ever talked to my pet like they were a human.",
            "Never have I ever forgotten why I walked into a room.",
            "Never have I ever lied about reading the Terms and Conditions.",
            "Never have I ever danced when nobody was watching.",
            "Never have I ever used a word in conversation that I didn't fully understand.",
            "Never have I ever pulled a push door (or pushed a pull door).",
            "Never have I ever said 'you too' when the waiter said 'enjoy your meal.'",
            "Never have I ever stayed up all night playing video games.",
            "Never have I ever pretended to know a song and just hummed along.",
            "Never have I ever accidentally called a teacher 'Mom' or 'Dad.'",
            "Never have I ever eaten an entire meal while standing in the kitchen.",
            "Never have I ever rehearsed a conversation in the shower.",
            "Never have I ever hit 'reply all' by accident.",
            "Never have I ever worn the same outfit two days in a row hoping nobody noticed.",
            "Never have I ever gone to the fridge, stared at it, closed it, and opened it again.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task NeverHaveIEver()
        {
            var prompt = _neverHaveIEver[_rng.Next(0, _neverHaveIEver.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🙅 Never Have I Ever")
                         .WithDescription(prompt)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 14. Roast — 30+ playful roasts
        // ────────────────────────────────────────────

        private static readonly string[] _roasts =
        [
            "You're like a cloud. Not because you're beautiful — because everything's brighter when you're gone.",
            "I'd roast you, but my mom told me not to burn trash.",
            "You bring everyone so much joy — when you log off.",
            "You're the reason God created the 'mute' button.",
            "You're not stupid. You just have bad luck thinking.",
            "If laughter is the best medicine, your face must be curing the world.",
            "You're like a software update: whenever I see you, I think 'not now.'",
            "I'd say you're a 10, but that's only your IQ.",
            "You're the human version of a typo.",
            "You're proof that even autocorrect gives up sometimes.",
            "Your family tree must be a cactus because everyone on it is a prick. Just kidding. Mostly.",
            "You're not the sharpest tool in the shed. Actually, you might not even be in the shed.",
            "You're like a penny: two-faced and found on the ground.",
            "If brains were rain, you'd be a desert.",
            "You have the personality of a speed bump.",
            "You're the type of person to get lost in your own house.",
            "If you were any more inbred, you'd be a sandwich.",
            "You're the human equivalent of a 'Wet Floor' sign.",
            "You bring a whole new meaning to 'NPC energy.'",
            "Your hairline is running away from your face faster than your ex.",
            "You're like off-brand cereal — technically fine, but nobody's first choice.",
            "You look like you got dressed in the dark. During an earthquake.",
            "If you were a candle, you'd be unscented.",
            "You have the charm of a damp sponge.",
            "You're the reason they invented the 'block' button.",
            "Your gaming skills are like your social skills — nonexistent.",
            "You peaked in the tutorial level.",
            "You're like lag in human form.",
            "You have main character energy — if the main character was an NPC.",
            "You're the background character who doesn't even have dialogue.",
            "If mediocrity had a mascot, it would be you. But in a lovable way.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Roast(IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;
            var roast = _roasts[_rng.Next(0, _roasts.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🔥 Roast")
                         .WithDescription($"{user.Mention}, {roast}")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 15. ShowerThought — 30+ shower thoughts
        // ────────────────────────────────────────────

        private static readonly string[] _showerThoughts =
        [
            "If you clean a vacuum cleaner, does that make you the vacuum cleaner?",
            "Your stomach thinks all potatoes are mashed.",
            "At some point, you were the youngest person on Earth.",
            "Nothing is on fire. Fire is on things.",
            "If you're waiting for the waiter, aren't you the waiter?",
            "Every mirror you buy is technically used.",
            "The word 'swims' upside down is still 'swims.'",
            "You've never seen your face, only reflections and photos of it.",
            "Your future self is watching you right now through memories.",
            "If two vegans are arguing, is it still considered beef?",
            "The 'S' in 'Island' is silent... just like the island wants to be.",
            "Dentists make money off of people with bad teeth. Why would I trust a product that 4 out of 5 dentists recommend?",
            "If you drop soap on the floor, is the floor clean or is the soap dirty?",
            "We drive on parkways and park on driveways.",
            "Your bed is a shelf for your body when you're not using it.",
            "History classes will only get harder over time.",
            "If poison expires, is it more poisonous or less poisonous?",
            "When you buy a bigger bed, you have more bed room but less bedroom.",
            "Aliens probably have conspiracy theories about us too.",
            "The letter 'W' is literally 'double u' but it looks like 'double v.'",
            "You never realize how long a minute is until you're doing a plank.",
            "Your phone has more computing power than the rocket that went to the moon.",
            "If you replace all the parts of a ship, is it still the same ship?",
            "Sleeping is just free trial death.",
            "We cook bacon and bake cookies.",
            "If life is unfair to everyone, doesn't that make it fair?",
            "The youngest photo of you is also the oldest photo of you.",
            "You are both the oldest and youngest you've ever been right now.",
            "Technically, we're all just brains piloting bone mechs wrapped in meat armor.",
            "If you dig a hole through the center of the earth and jump in, you'd be falling up at some point.",
            "Every time you clean something, you just make something else dirty.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ShowerThought()
        {
            var thought = _showerThoughts[_rng.Next(0, _showerThoughts.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🚿 Shower Thought")
                         .WithDescription(thought)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 16. DebateTopic — 30+ debate topics
        // ────────────────────────────────────────────

        private static readonly string[] _debateTopics =
        [
            "Is water wet?",
            "Should toilet paper hang over or under?",
            "Is a hot dog a sandwich?",
            "Is cereal soup?",
            "Should you pour milk or cereal first?",
            "Are Pop-Tarts ravioli?",
            "Is it better to be too hot or too cold?",
            "Should you eat the pizza crust?",
            "Is it okay to recline your seat on an airplane?",
            "Are video games a sport?",
            "Should the toilet seat be left up or down?",
            "Is it okay to put ketchup on a steak?",
            "Would you rather have no elbows or no knees?",
            "Is a taco a sandwich?",
            "Should you wash your legs in the shower or does the soapy water running down count?",
            "Is math discovered or invented?",
            "Should you eat breakfast or skip it?",
            "Is it better to be book smart or street smart?",
            "Should you shower at night or in the morning?",
            "Is Die Hard a Christmas movie?",
            "Should pizza be eaten with hands or a fork?",
            "Is it okay to double dip?",
            "Are people inherently good or evil?",
            "Should peanut butter be smooth or crunchy?",
            "Is the dress blue and black or white and gold?",
            "Is it better to have a lot of friends or a few close ones?",
            "Should you text back immediately or wait?",
            "Is pluto a planet?",
            "Should students have homework?",
            "Is it acceptable to use your phone during a movie?",
            "Should you fold or crumple toilet paper?",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task DebateTopic()
        {
            var topic = _debateTopics[_rng.Next(0, _debateTopics.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("⚔️ Debate Topic")
                         .WithDescription($"{topic}\n\nReact with 👍 or 👎 to cast your vote!")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 17. Motivation — 30+ motivational quotes
        // ────────────────────────────────────────────

        private static readonly string[] _motivationalQuotes =
        [
            "\"The only way to do great work is to love what you do.\" — Steve Jobs",
            "\"Believe you can and you're halfway there.\" — Theodore Roosevelt",
            "\"It does not matter how slowly you go, as long as you do not stop.\" — Confucius",
            "\"The future belongs to those who believe in the beauty of their dreams.\" — Eleanor Roosevelt",
            "\"Hardships often prepare ordinary people for an extraordinary destiny.\" — C.S. Lewis",
            "\"Success is not final, failure is not fatal: it is the courage to continue that counts.\" — Winston Churchill",
            "\"You are never too old to set another goal or to dream a new dream.\" — C.S. Lewis",
            "\"In the middle of every difficulty lies opportunity.\" — Albert Einstein",
            "\"The best time to plant a tree was 20 years ago. The second best time is now.\" — Chinese Proverb",
            "\"Don't watch the clock; do what it does. Keep going.\" — Sam Levenson",
            "\"Everything you've ever wanted is on the other side of fear.\" — George Addair",
            "\"You don't have to be perfect to be amazing.\" — Unknown",
            "\"Doubt kills more dreams than failure ever will.\" — Suzy Kassem",
            "\"The only impossible journey is the one you never begin.\" — Tony Robbins",
            "\"Your limitation — it's only your imagination.\" — Unknown",
            "\"Push yourself, because no one else is going to do it for you.\" — Unknown",
            "\"Great things never come from comfort zones.\" — Unknown",
            "\"Dream it. Wish it. Do it.\" — Unknown",
            "\"Wake up with determination. Go to bed with satisfaction.\" — Unknown",
            "\"Do something today that your future self will thank you for.\" — Sean Patrick Flanery",
            "\"Little things make big days.\" — Unknown",
            "\"It's going to be hard, but hard does not mean impossible.\" — Unknown",
            "\"Don't stop when you're tired. Stop when you're done.\" — Unknown",
            "\"You didn't come this far to only come this far.\" — Unknown",
            "\"The only limit to our realization of tomorrow will be our doubts of today.\" — Franklin D. Roosevelt",
            "\"Stars can't shine without darkness.\" — D.H. Sidebottom",
            "\"Be the change that you wish to see in the world.\" — Mahatma Gandhi",
            "\"What we think, we become.\" — Buddha",
            "\"You miss 100% of the shots you don't take.\" — Wayne Gretzky",
            "\"Life is 10% what happens to you and 90% how you react to it.\" — Charles R. Swindoll",
            "\"Strive not to be a success, but rather to be of value.\" — Albert Einstein",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Motivation()
        {
            var quote = _motivationalQuotes[_rng.Next(0, _motivationalQuotes.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("💪 Motivation")
                         .WithDescription(quote)
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 18. DadJoke — 30+ (setup + punchline)
        // ────────────────────────────────────────────

        private static readonly (string Setup, string Punchline)[] _dadJokes =
        [
            ("Why don't scientists trust atoms?", "Because they make up everything!"),
            ("I'm afraid for the calendar.", "Its days are numbered."),
            ("Why do fathers take an extra pair of socks when they go golfing?", "In case they get a hole in one."),
            ("What did the ocean say to the beach?", "Nothing, it just waved."),
            ("Why do bees have sticky hair?", "Because they use honeycombs."),
            ("What do you call a belt made of watches?", "A waist of time."),
            ("Why did the bicycle fall over?", "Because it was two-tired."),
            ("How does a penguin build its house?", "Igloos it together."),
            ("What do you call a fish without eyes?", "A fsh."),
            ("I used to hate facial hair.", "But then it grew on me."),
            ("What do sprinters eat before a race?", "Nothing — they fast."),
            ("Why can't a nose be 12 inches long?", "Because then it'd be a foot."),
            ("What does a baby computer call its father?", "Data."),
            ("I ordered a chicken and an egg from Amazon.", "I'll let you know."),
            ("What did the coffee report to the police?", "A mugging."),
            ("Why did the scarecrow win an award?", "He was outstanding in his field."),
            ("Why don't eggs tell jokes?", "They'd crack each other up."),
            ("What do you call a dog that does magic?", "A Labracadabrador."),
            ("What's brown and sticky?", "A stick."),
            ("How do trees access the internet?", "They log in."),
            ("Why did the invisible man turn down the job offer?", "He couldn't see himself doing it."),
            ("I once got fired from a canned juice company.", "Apparently I couldn't concentrate."),
            ("What do you call a factory that makes okay products?", "A satisfactory."),
            ("Why do chicken coops only have two doors?", "Because if they had four, they'd be chicken sedans."),
            ("What did one hat say to the other?", "Stay here, I'm going on ahead."),
            ("Why did the golfer bring two pairs of pants?", "In case he got a hole in one."),
            ("What did the janitor say when he jumped out of the closet?", "Supplies!"),
            ("I'm reading a book about anti-gravity.", "It's impossible to put down."),
            ("How do you make a tissue dance?", "Put a little boogie in it."),
            ("What do you call a sleeping dinosaur?", "A dino-snore."),
            ("Why did the math book look so sad?", "Because it had too many problems."),
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task DadJoke()
        {
            var (setup, punchline) = _dadJokes[_rng.Next(0, _dadJokes.Length)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("👨 Dad Joke")
                         .WithDescription($"{setup}\n\n||{punchline}||")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 19. PickRandom — pick random user
        // ────────────────────────────────────────────

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PickRandom()
        {
            var guildUser = (IGuildUser)ctx.User;
            var voiceChannel = guildUser.VoiceChannel;

            if (voiceChannel is not null)
            {
                var users = (await voiceChannel.GetUsersAsync().FlattenAsync())
                            .Where(u => !u.IsBot)
                            .ToList();

                if (users.Count == 0)
                {
                    await Response().Confirm("No non-bot users found in the voice channel.").SendAsync();
                    return;
                }

                var picked = users[_rng.Next(0, users.Count)];

                await Response()
                      .Embed(CreateEmbed()
                             .WithTitle("🎲 Random Pick (Voice Channel)")
                             .WithDescription($"From **{voiceChannel.Name}**, the chosen one is: {picked.Mention}!")
                             .WithOkColor())
                      .SendAsync();
                return;
            }

            // Fallback: pick from recent messages in text channel
            var messages = (await ctx.Channel.GetMessagesAsync(50).FlattenAsync())
                           .Select(m => m.Author)
                           .Where(a => !a.IsBot)
                           .DistinctBy(a => a.Id)
                           .ToList();

            if (messages.Count == 0)
            {
                await Response().Confirm("No users found to pick from.").SendAsync();
                return;
            }

            var pickedUser = messages[_rng.Next(0, messages.Count)];

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("🎲 Random Pick (Text Channel)")
                         .WithDescription($"From recent messages, the chosen one is: {pickedUser.Mention}!")
                         .WithOkColor())
                  .SendAsync();
        }

        // ────────────────────────────────────────────
        // 20. Rate — rate anything 0-10
        // ────────────────────────────────────────────

        private static readonly string[] _rateComments =
        [
            /* 0 */  "Absolutely terrible. I'd rather watch paint dry. On a wall that doesn't exist.",
            /* 1 */  "One star, like a participation award. You showed up, and that's about it.",
            /* 2 */  "Two out of ten. At least it's not nothing... but it's close.",
            /* 3 */  "Three. It's giving 'microwave dinner' energy.",
            /* 4 */  "Four. It tried. Bless its heart.",
            /* 5 */  "A perfect five. Completely, utterly, aggressively mid.",
            /* 6 */  "Six out of ten. Not bad, not great. Like a Tuesday.",
            /* 7 */  "Seven! Solid. Like finding $5 in your pocket.",
            /* 8 */  "Eight out of ten! Now we're cooking with gas!",
            /* 9 */  "Nine?! Almost perfection. Chef's kiss.",
            /* 10 */ "A PERFECT TEN! Absolutely flawless. No notes. Standing ovation.",
        ];

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Rate([Leftover] string thing = null)
        {
            if (string.IsNullOrWhiteSpace(thing))
            {
                await Response().Confirm("Give me something to rate! Usage: `.fun rate <anything>`").SendAsync();
                return;
            }

            // Deterministic rating based on the input string so the same thing always gets the same score
            var hash = thing.ToLowerInvariant().GetHashCode();
            var rating = Math.Abs(hash % 11);
            var comment = _rateComments[rating];
            var stars = new string('⭐', rating) + new string('☆', 10 - rating);

            await Response()
                  .Embed(CreateEmbed()
                         .WithTitle("📊 Rating Machine")
                         .AddField("Subject", thing)
                         .AddField("Rating", $"**{rating}/10**\n{stars}")
                         .AddField("Verdict", comment)
                         .WithOkColor())
                  .SendAsync();
        }
    }
}
