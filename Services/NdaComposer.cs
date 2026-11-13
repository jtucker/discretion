using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Discretion.Models;

namespace Discretion.Services;

/// <summary>Assembles the legal text of an agreement into a renderer-agnostic document model.</summary>
public static class NdaComposer
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string LongDate(DateTime d) => d.ToString("MMMM d, yyyy", Us);

    public static AgreementDocument Compose(Agreement a)
    {
        var template = TemplateCatalog.Find(a.TemplateId)
            ?? throw new InvalidOperationException($"Unknown template '{a.TemplateId}'.");
        var dp = a.DisclosingParty;
        var rp = a.ReceivingParty;
        var law = UsStates.Jurisdiction(a.GoverningState);

        var blocks = new List<DocBlock>
        {
            new TitleBlock("NON-DISCLOSURE AGREEMENT", template.Title, a.ReferenceNumber),
            new ParagraphBlock(
            [
                new("This Non-Disclosure Agreement (this \"Agreement\") is entered into and made effective as of "),
                new(LongDate(a.EffectiveDate), RunStyle.Bold),
                new(" (the \"Effective Date\"), by and between "),
                new(dp.Name, RunStyle.Bold),
                new(Residing(dp) + " (the \"Disclosing Party\"), and "),
                new(rp.Name, RunStyle.Bold),
                new(Residing(rp) + " (the \"Receiving Party\"). The Disclosing Party and the Receiving Party are each referred to herein as a \"Party\" and collectively as the \"Parties\".")
            ]),
            new SectionHeadingBlock("RECITALS"),
            new ParagraphBlock($"WHEREAS, {template.Recital}; and"),
            new ParagraphBlock("WHEREAS, the Disclosing Party has disclosed, or may hereafter disclose, certain information to the Receiving Party, and the Receiving Party is, or may become, aware of such information by virtue of having been present, nearby, or insufficiently distracted; and"),
            new ParagraphBlock("WHEREAS, the Parties desire to set forth in writing the terms upon which such information shall be held in the strictest confidence;"),
            new ParagraphBlock("NOW, THEREFORE, in consideration of the mutual covenants contained herein, the continuation of the Parties' relationship, and other good and valuable consideration, the receipt and sufficiency of which are hereby acknowledged, the Parties agree as follows:"),
            new SectionHeadingBlock("TERMS AND CONDITIONS"),
            Clause(1, "Confidential Information",
                $"For purposes of this Agreement, \"Confidential Information\" means {template.Definition}. Confidential Information includes information in any form or medium, whether disclosed before or after the Effective Date, and whether or not marked or designated as confidential."),
            Clause(2, "Obligations of the Receiving Party",
                "The Receiving Party shall (a) hold the Confidential Information in the strictest confidence; (b) not disclose, publish, broadcast, post, hint at, or otherwise make available the Confidential Information to any third party, by any means now known or hereafter devised; (c) exercise at least the same degree of care to protect the Confidential Information that the Receiving Party uses to protect their own most embarrassing information, and in no event less than a reasonable degree of care; and (d) promptly notify the Disclosing Party of any actual or suspected unauthorized disclosure."),
            Clause(3, "Exclusions",
                "The obligations of this Agreement shall not apply to information that the Receiving Party can demonstrate by clear and convincing evidence (a) was publicly known through no fault of the Receiving Party; (b) was lawfully known to the Receiving Party prior to disclosure without any obligation of confidentiality; or (c) is required to be disclosed by a court of competent jurisdiction, provided that the Receiving Party gives the Disclosing Party prompt written notice and an opportunity to seek a protective order, change their name, or relocate."),
            Clause(4, "Term",
                $"This Agreement shall commence on the Effective Date and remain in full force and effect for a period of {a.TermDescription}, expiring on {LongDate(a.ExpirationDate)} (the \"Term\"), unless earlier terminated by mutual written agreement of the Parties."),
            Clause(5, "Survival", a.SurvivesExpiration
                ? "Notwithstanding the expiration or termination of this Agreement, the obligations of the Receiving Party with respect to the Confidential Information shall survive the expiration of the Term and shall continue in full force and effect indefinitely, until such time as the Confidential Information becomes publicly known through no fault of the Receiving Party or the Disclosing Party provides an express written release, whichever occurs first."
                : "Upon the expiration of the Term, the obligations of the Receiving Party with respect to the Confidential Information shall terminate, and the Receiving Party shall thereafter be released from the obligations of Section 2; provided, however, that the Receiving Party shall continue to exercise good judgment, common decency, and appropriate restraint at family gatherings. Any breach occurring prior to expiration shall survive and remain actionable."),
            Clause(6, "Special Covenants",
                "In addition to the obligations set forth above, the Receiving Party specifically covenants and agrees as follows:"),
        };

        var letter = 'a';
        foreach (var covenant in template.SpecialCovenants)
            blocks.Add(new ParagraphBlock([new($"({letter++})  "), new(covenant)], Indent: 18));

        blocks.AddRange(
        [
            Clause(7, "Return and Destruction of Materials",
                "Upon the written request of the Disclosing Party, the Receiving Party shall promptly return or destroy all materials embodying the Confidential Information, including photographs, recordings, screenshots, notes, receipts, and souvenirs, together with all copies thereof, and shall certify such destruction in writing if so requested."),
            Clause(8, "No License; No Obligation",
                "Nothing in this Agreement grants the Receiving Party any right, title, license, or interest in the Confidential Information, including any right to adapt the Confidential Information into a podcast, memoir, screenplay, stand-up routine, or anecdote at a dinner party."),
            Clause(9, "Remedies",
                "The Receiving Party acknowledges that any breach of this Agreement may cause the Disclosing Party irreparable harm, including reputational, social, and emotional harm, for which monetary damages would be an inadequate remedy. Accordingly, the Disclosing Party shall be entitled to seek injunctive relief and specific performance, in addition to any other remedies available at law or in equity, without the necessity of posting bond."),
            Clause(10, "Governing Law and Venue",
                $"This Agreement shall be governed by and construed in accordance with the laws of {law}, without regard to its conflict of laws principles. Each Party irrevocably submits to the exclusive jurisdiction of the state and federal courts located in {law} for the resolution of any dispute arising out of or relating to this Agreement."),
            Clause(11, "Entire Agreement; Amendment",
                "This Agreement constitutes the entire agreement between the Parties with respect to its subject matter and supersedes all prior understandings, pinky promises, and assurances that \"it's fine, I won't say anything.\" This Agreement may be amended only by a written instrument executed by both Parties."),
            Clause(12, "Severability; Waiver",
                "If any provision of this Agreement is held invalid or unenforceable, such provision shall be enforced to the maximum extent permissible and the remaining provisions shall continue in full force and effect. No waiver of any breach shall be deemed a waiver of any subsequent breach."),
            Clause(13, "Counterparts; Electronic Signatures",
                "This Agreement may be executed in counterparts, each of which shall be deemed an original, and all of which together shall constitute one instrument. The Parties consent to the use of electronic signatures, which shall have the same force and effect as original handwritten signatures."),
            new ParagraphBlock("IN WITNESS WHEREOF, the Parties have executed this Agreement as of the Effective Date first written above."),
        ]);

        blocks = blocks.Select(Typeset).ToList();
        var fingerprint = Fingerprint(blocks, a);

        blocks.Add(new SignatureBlock("DISCLOSING PARTY", dp, a.DisclosingSignature));
        blocks.Add(new SignatureBlock("RECEIVING PARTY", rp, a.ReceivingSignature));

        return new AgreementDocument { ReferenceNumber = a.ReferenceNumber, Fingerprint = fingerprint, Blocks = blocks };
    }

    /// <summary>Generates a reference number such as NDA-2026-7KQ4XM.</summary>
    public static string NewReferenceNumber(DateTime effective)
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        Span<char> code = stackalloc char[6];
        for (var i = 0; i < code.Length; i++)
            code[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return $"NDA-{effective:yyyy}-{new string(code)}";
    }

    private static DocBlock Typeset(DocBlock block) => block switch
    {
        ParagraphBlock p => p with { Runs = SmartQuotes(p.Runs) },
        _ => block
    };

    /// <summary>Converts straight quotes to typographic quotes across a paragraph's runs.</summary>
    private static IReadOnlyList<TextRun> SmartQuotes(IReadOnlyList<TextRun> runs)
    {
        var prev = ' ';
        var result = new List<TextRun>(runs.Count);
        foreach (var run in runs)
        {
            var chars = run.Text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var opening = char.IsWhiteSpace(prev) || prev is '(' or '[' or '\u2014';
                chars[i] = chars[i] switch
                {
                    '"' => opening ? '\u201C' : '\u201D',
                    '\'' => opening ? '\u2018' : '\u2019',
                    _ => chars[i]
                };
                prev = chars[i];
            }
            result.Add(run with { Text = new string(chars) });
        }
        return result;
    }

    private static ParagraphBlock Clause(int number, string heading, string body) =>
        new([new($"{number}.  {heading}. ", RunStyle.Bold), new(body)]);

    private static string Residing(PartyInfo p) =>
        string.IsNullOrWhiteSpace(p.FormattedAddress) ? "" : $", an individual residing at {p.FormattedAddress}";

    private static string Fingerprint(IEnumerable<DocBlock> blocks, Agreement a)
    {
        var sb = new StringBuilder();
        foreach (var b in blocks)
        {
            switch (b)
            {
                case TitleBlock t: sb.Append(t.Title).Append(t.Subtitle).Append(t.Reference); break;
                case SectionHeadingBlock h: sb.Append(h.Text); break;
                case ParagraphBlock p: foreach (var r in p.Runs) sb.Append(r.Text); break;
            }
            sb.Append('\n');
        }
        sb.Append(a.ReceivingParty.Email).Append(a.ReceivingParty.Phone);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..16];
    }
}
