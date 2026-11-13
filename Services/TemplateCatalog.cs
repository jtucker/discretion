using Discretion.Models;

namespace Discretion.Services;

public static class TemplateCatalog
{
    public static IReadOnlyList<NdaTemplate> All { get; } =
    [
        new()
        {
            Id = "confidential-discussion",
            Title = "Confidential Discussion",
            Category = "General Purpose",
            Monogram = "CD",
            Summary = "A standard instrument covering a private conversation of any nature, including those prefaced with \"don't tell anyone, but\".",
            Recital = "the Disclosing Party wishes to share with the Receiving Party certain information during a private conversation, which conversation may have been prefaced with the words \"don't tell anyone, but\" or a substantially similar phrase (the \"Discussion\")",
            Definition = "any and all information disclosed during the Discussion, whether oral, written, whispered, mouthed, or communicated through facial expression, together with the fact that the Discussion occurred at all",
            SpecialCovenants =
            [
                "The Receiving Party shall not preface any subsequent disclosure of the Confidential Information with the words \"I probably shouldn't say this, but\", and acknowledges that such words do not operate as a waiver of any obligation hereunder.",
                "The Receiving Party shall not disclose the Confidential Information to a spouse, partner, or best friend under the mistaken belief that such persons \"don't count\". They count."
            ]
        },
        new()
        {
            Id = "extramarital-affair",
            Title = "Post-Extramarital Affair",
            Category = "Domestic Relations",
            Monogram = "EA",
            Summary = "Protects the particulars of certain romantic conduct occurring outside the bounds of a marriage or committed relationship.",
            Recital = "the Receiving Party participated in, witnessed, or otherwise became aware of certain romantic, amorous, or otherwise ill-advised conduct occurring outside the bounds of a marriage or committed relationship (the \"Incident\")",
            Definition = "all information relating to the Incident, including without limitation the identities of all participants, dates, times, locations, hotel folios, restaurant reservations, ride-share records, credit card statements, text messages, voicemails, gift receipts, and the existence of the Incident itself",
            SpecialCovenants =
            [
                "The Receiving Party shall not disclose the Confidential Information directly or by implication, including by means of raised eyebrows, knowing glances, prolonged eye contact, or pointed remarks at family gatherings, holiday functions, or weddings.",
                "Should the Receiving Party be questioned by a spouse, partner, relative, or licensed private investigator, the Receiving Party shall respond exclusively with the phrase \"I wouldn't know anything about that.\"",
                "The Receiving Party shall permanently delete all photographs, messages, and location-sharing histories relating to the Incident within forty-eight (48) hours of the Effective Date, including any copies residing in so-called \"cloud\" storage."
            ]
        },
        new()
        {
            Id = "substance-use",
            Title = "Recreational Substance Use",
            Category = "Personal Conduct",
            Monogram = "RS",
            Summary = "Covers the consumption of certain substances and the regrettable decisions made under their influence.",
            Recital = "the Parties were present at an occasion during which certain substances were consumed, ingested, inhaled, or otherwise enjoyed, and during which certain decisions were made that the Disclosing Party now regrets (the \"Event\")",
            Definition = "all information concerning the Event, including the nature and quantity of any substance involved, the identity of any supplier, all statements made by the Disclosing Party regarding the nature of the universe, any attempt to communicate with animals or appliances, and any subsequent order placed with a food delivery service",
            SpecialCovenants =
            [
                "The Receiving Party shall not reference the Event in any job interview, reference check, custody proceeding, wedding toast, or Thanksgiving dinner.",
                "The Parties stipulate that any recollection of the Event held by either Party is inherently unreliable, and agree that any such recollection shall be deemed inadmissible in any forum, including group chats.",
                "Nothing in this Agreement shall be construed as an endorsement of the conduct described herein, nor as an admission that such conduct occurred."
            ]
        },
        new()
        {
            Id = "bachelor-party",
            Title = "Bachelor / Bachelorette Party",
            Category = "Celebrations",
            Monogram = "BP",
            Summary = "Codifies the time-honored principle that what happens at the celebration stays at the celebration.",
            Recital = "the Receiving Party attended a pre-nuptial celebration held in honor of a soon-to-be-married individual (the \"Celebration\"), and the Parties wish to formalize the customary understanding that what happens at the Celebration stays at the Celebration",
            Definition = "all events, conduct, expenditures, and costume decisions occurring during the Celebration, including any activity occurring in a limousine, party bus, casino, karaoke establishment, or emergency room, and the contents of any shared photo album",
            SpecialCovenants =
            [
                "The Receiving Party shall not post, story, reel, tweet, or otherwise publish any image or recording from the Celebration, including images in which the Receiving Party believes they personally look \"really good\".",
                "The Receiving Party shall not disclose to the betrothed, the betrothed's family, or any officiant the final amount spent on bottle service.",
                "Matching apparel acquired for the Celebration shall be destroyed or retained in strict confidence, and shall not be worn in public on any subsequent occasion."
            ]
        },
        new()
        {
            Id = "karaoke",
            Title = "Karaoke Performance",
            Category = "Performing Arts",
            Monogram = "KP",
            Summary = "Shields a vocal performance of questionable merit, with particular regard to power ballads.",
            Recital = "the Disclosing Party delivered one or more vocal performances at a karaoke establishment, private residence, or vehicle (the \"Performance\"), the artistic merit of which remains in dispute",
            Definition = "all audio and video recordings of the Performance, the song selections made, any choreography attempted, any key changes missed, and the number of times the Disclosing Party performed \"Total Eclipse of the Heart\"",
            SpecialCovenants =
            [
                "The Receiving Party shall not describe the Performance to any third party using the words \"brave\", \"interesting\", or \"a lot\".",
                "The Receiving Party shall not request an encore of the Performance at any future social gathering."
            ]
        },
        new()
        {
            Id = "late-night-texts",
            Title = "Late-Night Text Messages",
            Category = "Communications",
            Monogram = "LT",
            Summary = "Covers messages transmitted after 11:00 p.m., which the Disclosing Party may or may not recall sending.",
            Recital = "the Disclosing Party transmitted one or more electronic messages to the Receiving Party between the hours of 11:00 p.m. and 4:00 a.m. (the \"Messages\"), the content of which the Disclosing Party now disavows",
            Definition = "the Messages and their contents, including any declarations of affection, unsolicited opinions, voice memos, typographical errors, emoji, and the phrase \"u up?\"",
            SpecialCovenants =
            [
                "The Receiving Party shall not capture, forward, or share screenshots of the Messages, and shall not read the Messages aloud for dramatic effect.",
                "The Receiving Party acknowledges that the Messages were sent under the influence of fatigue and do not reflect the Disclosing Party's true opinions, feelings, or spelling ability."
            ]
        },
        new()
        {
            Id = "browser-history",
            Title = "Browser History Exposure",
            Category = "Technology",
            Monogram = "BH",
            Summary = "For when someone borrowed your laptop and saw more than they bargained for.",
            Recital = "the Receiving Party, while using a computing device belonging to the Disclosing Party, inadvertently observed the Disclosing Party's browsing history, search queries, or open tabs (the \"Exposure\")",
            Definition = "all information observed during the Exposure, including search queries, autocomplete suggestions, bookmarks, open tabs, shopping carts, and the number of hours of video content viewed on any single topic",
            SpecialCovenants =
            [
                "The Receiving Party shall not reference any search query observed during the Exposure, whether directly or by means of oddly specific gift-giving.",
                "The Receiving Party agrees that any search query observed during the Exposure was conducted \"for a friend\"."
            ]
        },
        new()
        {
            Id = "restroom-incident",
            Title = "Restroom Incident",
            Category = "Medical & Personal",
            Monogram = "RI",
            Summary = "Protects the details of an unfortunate gastrointestinal event and its aftermath.",
            Recital = "the Receiving Party was present for, or in auditory or olfactory proximity to, a gastrointestinal event experienced by the Disclosing Party (the \"Incident\")",
            Definition = "all sensory information relating to the Incident, including its duration, volume, aroma, and location, the identity of the plumbing fixture involved, and any remedial measures undertaken, including the use of air fresheners, matches, or a plunger",
            SpecialCovenants =
            [
                "The Receiving Party shall not refer to the Incident by any nickname, code word, or descriptive title, including those of a seismic or meteorological nature.",
                "The Receiving Party shall not alert any subsequent user of the affected facility, and waives any right to post signage."
            ]
        },
        new()
        {
            Id = "family-recipe",
            Title = "Secret Family Recipe",
            Category = "Trade Secrets",
            Monogram = "FR",
            Summary = "Safeguards a closely held culinary formula of significant sentimental and competitive value.",
            Recital = "the Disclosing Party is the custodian of a closely held family recipe of significant sentimental and competitive value (the \"Recipe\"), and wishes to share the Recipe with the Receiving Party for the sole purpose of preparing it",
            Definition = "the Recipe in its entirety, including all ingredients, quantities, substitutions, preparation techniques, cooking times, the identity of the secret ingredient, and the fact that the Recipe originated on the back of a soup can",
            SpecialCovenants =
            [
                "The Receiving Party shall not enter the Recipe, or any dish prepared therefrom, in any cook-off, potluck competition, bake sale, or county fair without the prior written consent of the Disclosing Party.",
                "If complimented on any dish prepared from the Recipe, the Receiving Party shall respond only, \"It's an old family secret,\" and shall not identify which family."
            ]
        },
        new()
        {
            Id = "surprise-party",
            Title = "Surprise Party",
            Category = "Celebrations",
            Monogram = "SP",
            Summary = "Binds the Receiving Party to silence regarding a forthcoming surprise celebration.",
            Recital = "the Disclosing Party is organizing a surprise celebration in honor of a third party (the \"Honoree\") and requires the Receiving Party's assistance and silence in connection therewith (the \"Party\")",
            Definition = "the existence, date, time, location, guest list, theme, and catering arrangements of the Party, together with any cover story devised to secure the Honoree's attendance",
            SpecialCovenants =
            [
                "The Receiving Party shall not act suspiciously in the presence of the Honoree, and shall refrain from winking, smirking, or asking the Honoree about their plans for the date of the Party.",
                "The Receiving Party shall arrive at the Party no later than thirty (30) minutes prior to the Honoree and shall park out of sight."
            ]
        },
        new()
        {
            Id = "workplace-romance",
            Title = "Workplace Romance",
            Category = "Employment",
            Monogram = "WR",
            Summary = "Maintains the discretion of a romantic relationship between colleagues that HR does not need to know about yet.",
            Recital = "the Receiving Party has become aware of a romantic relationship between two or more colleagues employed by the same organization (the \"Relationship\") which has not been disclosed to human resources, management, or the break room",
            Definition = "all information concerning the Relationship, including the identities of the parties thereto, the coordinated timing of their arrivals and departures, all shared lunches, and all activity in the supply closet",
            SpecialCovenants =
            [
                "The Receiving Party shall not disclose the Confidential Information at any team meeting, offsite, retreat, or holiday party, and shall not speculate as to why two colleagues took the same afternoon off.",
                "The Receiving Party shall maintain a neutral facial expression whenever the parties to the Relationship are present in the same meeting."
            ]
        },
        new()
        {
            Id = "spoilers",
            Title = "Unreleased Entertainment Spoilers",
            Category = "Media & Entertainment",
            Monogram = "US",
            Summary = "Prevents disclosure of plot developments for television, film, or literature not yet consumed by the Disclosing Party.",
            Recital = "the Disclosing Party has not yet consumed certain works of television, film, or literature (the \"Works\") which the Receiving Party has consumed, and the Disclosing Party wishes to experience the Works without prior knowledge of their contents",
            Definition = "all plot developments, character deaths, romantic pairings, twist endings, post-credits scenes, and general vibes of the Works, including any statement of the form \"just wait until episode six\"",
            SpecialCovenants =
            [
                "The Receiving Party shall not gasp, smile knowingly, or say \"oh, you're in for it\" in the presence of the Disclosing Party when the Works are discussed.",
                "The obligations herein shall apply equally to the Receiving Party's social media activity, including vague posts intended to be understood by \"those who know\"."
            ]
        },
        new()
        {
            Id = "self-haircut",
            Title = "Self-Administered Haircut",
            Category = "Personal Grooming",
            Monogram = "SH",
            Summary = "Protects the origin story of the Disclosing Party's current hairstyle.",
            Recital = "the Disclosing Party performed a haircut upon themselves without professional assistance (the \"Procedure\"), and the Receiving Party witnessed the Procedure or its immediate aftermath",
            Definition = "all information relating to the Procedure, including the implements used, any instructional video consulted, the number of corrective attempts made, and any photographs taken prior to the purchase of a hat",
            SpecialCovenants =
            [
                "Should any third party inquire as to the Disclosing Party's hairstyle, the Receiving Party shall state that it was performed by \"a stylist downtown\" and decline to provide further particulars.",
                "The Receiving Party shall not compare the Disclosing Party's hairstyle to that of any historical figure, cartoon character, or household pet."
            ]
        }
    ];

    public static NdaTemplate? Find(string id) => All.FirstOrDefault(t => t.Id == id);
}
