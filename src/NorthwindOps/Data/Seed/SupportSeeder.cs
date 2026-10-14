using Microsoft.EntityFrameworkCore;

namespace NorthwindOps.Data.Seed;

/// <summary>
/// Support tickets for the Support screen: about 40 everyday tickets from Northwind customers, plus three
/// long-running threads (an order-sync incident log, a cold-chain investigation and a contract negotiation)
/// with hundreds of messages between them, from one-liners to several paragraphs. Deterministic, and dated
/// relative to today like the orders.
/// </summary>
public sealed class SupportSeeder : IDemoDataSeeder
{
    public const int FirstTicketId = 2401;

    static readonly string[] Agents = ["Laura Callahan", "Michael Suyama", "Anne Dodsworth", "Janet Leverling"];

    public async Task SeedAsync(NorthwindDb db, DateTime today, CancellationToken ct)
    {
        var random = new Random(2401);
        var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Id).Select(p => p.Name).ToListAsync(ct);
        var recentOrders = await db.Orders.AsNoTracking()
            .Where(o => o.PlacedOn >= today.AddDays(-60) && o.PlacedOn < today.AddDays(-3) && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.Id)
            .Select(o => new SeedOrder(o.Id, o.CustomerId, o.PlacedOn, o.Lines.OrderBy(l => l.Id).Select(l => l.Product.Name).First()))
            .ToListAsync(ct);

        var bigThreadCustomers = new[] { "SAVEA", "RATTC", "ERNSH" };
        var smallTicketOrders = recentOrders
            .Where(o => !bigThreadCustomers.Contains(o.CustomerId))
            .OrderBy(_ => random.Next())
            .DistinctBy(o => o.CustomerId)
            .Take(37)
            .ToList();

        var tickets = new List<Ticket>();
        for (var i = 0; i < smallTicketOrders.Count; i++)
        {
            tickets.Add(SmallTicket(i, smallTicketOrders[i], customers[smallTicketOrders[i].CustomerId], products, today, random));
        }

        tickets.Add(IncidentTicket(customers["SAVEA"], recentOrders.LastOrDefault(o => o.CustomerId == "SAVEA"), today, new Random(11)));
        tickets.Add(ColdChainTicket(customers["RATTC"], today, new Random(12)));
        tickets.Add(ContractTicket(customers["ERNSH"], today, new Random(13)));

        // Ids follow the order the tickets were opened in, like a real ticket system.
        var id = FirstTicketId;
        foreach (var ticket in tickets.OrderBy(t => t.OpenedOn).ThenBy(t => t.Subject))
        {
            ticket.Id = id++;
        }

        db.Set<Ticket>().AddRange(tickets);
    }

    // ───────────── Everyday tickets ─────────────

    static Ticket SmallTicket(int index, SeedOrder order, Customer customer, List<string> products, DateTime today, Random random)
    {
        var scenario = Scenarios[index % Scenarios.Length];
        var agent = Agents[index % Agents.Length];
        var contact = customer.Contact ?? customer.Company;
        var otherProduct = products.Where(p => p != order.Product).ElementAt(random.Next(products.Count - 1));
        var quantity = random.Next(2, 9) * 5;
        var tokens = new Dictionary<string, string>
        {
            ["{contact}"] = contact,
            ["{first}"] = contact.Split(' ')[0],
            ["{company}"] = customer.Company,
            ["{city}"] = customer.City ?? "your",
            ["{order}"] = order.Id.ToString(),
            ["{product}"] = order.Product,
            ["{product2}"] = otherProduct,
            ["{qty}"] = quantity.ToString(),
            ["{short}"] = (quantity - quantity / 5).ToString(),
            ["{agent}"] = agent,
        };

        // A third of the tickets are finished, a third are waiting on the customer, a third need us.
        var stage = (index % 3) switch { 0 => TicketStatus.Resolved, 1 => TicketStatus.Open, _ => TicketStatus.Waiting };
        var script = stage switch
        {
            TicketStatus.Resolved => scenario.Script,
            TicketStatus.Open => CutAfterLast(scenario.Script, MessageAuthor.Customer, random),
            _ => CutAfterLast(scenario.Script, MessageAuthor.Agent, random),
        };
        if (stage == TicketStatus.Open && random.Next(3) == 0)
        {
            script = [.. script, (MessageAuthor.Customer, Chasers[random.Next(Chasers.Length)])];
        }

        var gaps = script.Select((_, i) => i == 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(random.Next(20, 60 * 26))).ToList();
        var span = gaps.Aggregate(TimeSpan.Zero, (sum, gap) => sum + gap);
        var latestEnd = today.AddHours(-random.Next(2, 30));
        var start = Min(order.PlacedOn.AddDays(random.Next(2, 6)).AddHours(random.Next(-3, 4)), latestEnd - span);

        var messages = new List<TicketMessage>();
        var clock = start;
        for (var i = 0; i < script.Length; i++)
        {
            clock += gaps[i];
            var (kind, template) = script[i];
            messages.Add(new TicketMessage
            {
                Number = i + 1,
                AuthorKind = kind,
                Author = kind == MessageAuthor.Customer ? contact : agent,
                SentOn = clock,
                Body = Fill(template, tokens),
            });
        }

        // Open tickets usually have a reply nobody has read yet.
        var unread = stage == TicketStatus.Open ? Math.Min(messages.Count - 1, random.Next(0, 3)) : 0;
        return new Ticket
        {
            Subject = Fill(scenario.Subject, tokens),
            CustomerId = customer.Id,
            OrderId = scenario.Subject.Contains("{order}") || scenario.Script[0].Body.Contains("{order}") ? order.Id : null,
            Status = stage,
            Priority = scenario.Priority,
            OpenedOn = messages[0].SentOn,
            LastActivityOn = messages[^1].SentOn,
            ReadUpTo = messages.Count - unread,
            Messages = messages,
        };
    }

    static (MessageAuthor Kind, string Body)[] CutAfterLast((MessageAuthor Kind, string Body)[] script, MessageAuthor author, Random random)
    {
        // End the thread at one of the author's messages, but never on the very first or the closing one.
        var candidates = Enumerable.Range(0, script.Length - 1).Where(i => script[i].Kind == author).ToList();
        var end = candidates.Count == 0 ? 1 : candidates[random.Next(candidates.Count)];
        return script[..(end + 1)];
    }

    sealed record Scenario(string Subject, TicketPriority Priority, (MessageAuthor Kind, string Body)[] Script);

    const MessageAuthor C = MessageAuthor.Customer;
    const MessageAuthor A = MessageAuthor.Agent;

    static readonly string[] Chasers =
    [
        "Any update on this?",
        "Hi, just chasing this one. We need an answer before our next order.",
        "Hello? We still haven't heard back.",
        "Following up on the above. Could someone call me this afternoon?",
    ];

    static readonly Scenario[] Scenarios =
    [
        new("Order #{order} hasn't arrived", TicketPriority.High,
        [
            (C, "Hi, our order #{order} was due yesterday and nothing has turned up. We've got {product} on the menu this weekend, so I need to know today whether it's coming.\n\nCan you check where it is?"),
            (A, "Hi {first}, sorry about this. I've checked with the warehouse: #{order} left us on time, but the carrier missed the connection at the hub overnight. It's on this morning's van and the tracking now says delivery before 2pm.\n\nI'll keep an eye on it and let you know if that changes."),
            (C, "OK, thanks. If it isn't here by 2 we'll have to buy locally."),
            (A, "Understood. If it's late again I'll refund the delivery charge and cover the difference on anything you have to buy in. Just send me the receipt."),
            (C, "It arrived at 1:20. Everything's there."),
            (A, "Glad to hear it. I've refunded the delivery charge on #{order} anyway; you'll see it on your next statement. Closing this one."),
        ]),
        new("Damaged cases on order #{order}", TicketPriority.Normal,
        [
            (C, "Two cases of {product} from order #{order} arrived crushed. The outer cartons were wet and several units inside are split. Photos attached.\n\nWe've kept everything to one side. Do you want it back, or can we throw it away?"),
            (A, "Thanks for the photos, {first}. That looks like water damage in transit rather than a packing problem, so it's on us and the carrier.\n\nPlease dispose of the damaged units; there's no need to send them back. I've raised a credit for the two cases and put replacements on your next delivery."),
            (C, "How much is the credit? Our accounts team will ask."),
            (A, "It's the full invoice value of the two cases, including tax. The credit note number will be in the email you get from our accounts team within a day."),
            (C, "Credit note received. Thanks for sorting this out so quickly."),
            (A, "No problem. I've also reported the delivery to the carrier so they check how that van is loaded."),
        ]),
        new("Invoice for #{order} doesn't match our PO", TicketPriority.Normal,
        [
            (C, "Hello,\n\nThe invoice we received for order #{order} doesn't match our purchase order. The PO had {product} at the price from your spring list, but the invoice charges the new price. The difference across the order is small, but our system won't approve an invoice that doesn't match the PO.\n\nCould you reissue it at the PO price, or send us a credit for the difference? Whichever is easier for you.\n\nKind regards,\n{contact}\n{company}"),
            (A, "Hi {first}, you're right. The order was placed two days before the new prices took effect, but it was invoiced after, and our billing run used the invoice date. I'll ask accounts to cancel it and reissue it at the PO price."),
            (A, "Accounts have reissued the invoice. The new one replaces the old one completely, so please ignore the first."),
            (C, "Got it, and it's approved. Will the same thing happen to the other two orders we placed that week?"),
            (A, "I've checked: both of those were invoiced at the old price, so they're fine."),
        ]),
        new("Short shipped: {product}", TicketPriority.Normal,
        [
            (C, "We ordered {qty} of {product} on #{order} and only received {short}. The delivery note says {qty}."),
            (A, "Hi {first}, the pick list shows the full quantity was picked, so I've asked the warehouse to check the dispatch scan. Can you confirm how many cartons arrived in total?"),
            (C, "Four cartons. The note says five."),
            (A, "Thanks, that's what I needed. The fifth carton was found on the loading dock this morning: it missed the van. It's going out on tomorrow's delivery at no charge."),
            (C, "Great, thanks."),
        ]),
        new("Received {product2} instead of {product}", TicketPriority.Normal,
        [
            (C, "Our delivery today had {product2} where we ordered {product}. Can you swap them?"),
            (A, "Sorry about that, {first}. The two are next to each other in the warehouse and their codes are one digit apart. The driver will collect the {product2} and drop off the {product} tomorrow morning. Does a 7–9am slot work?"),
            (C, "7–9 is fine."),
            (C, "The driver came and swapped them. All good."),
            (A, "Thanks for confirming. I've asked the warehouse to move those two products apart."),
        ]),
        new("Allergen sheet for {product}", TicketPriority.Low,
        [
            (C, "Can you send the current allergen and ingredients sheet for {product}? Our food safety inspection is next week and the one we have on file is from last year."),
            (A, "Hi {first}, I've asked the supplier for their latest specification. They usually reply within two working days, so you'll have it before the inspection."),
            (C, "Thanks. Is there anything new in it? We list allergens on our menu."),
            (A, "The supplier has confirmed one change: the product is now made on a line that also handles tree nuts, so it carries a 'may contain' warning. The recipe itself hasn't changed. The full sheet is attached."),
            (C, "That's exactly what we needed to know. We'll update the menu."),
        ]),
        new("Can we move our deliveries to mornings?", TicketPriority.Low,
        [
            (C, "We're changing our opening hours from next month and won't have anyone in after 2pm. Is it possible to move our deliveries to the morning?"),
            (A, "Hi {first}, our {city} route runs in the morning on Tuesdays and Fridays. If those days work, I can move you over from the 1st. Your standing order would move with it."),
            (C, "Tuesday and Friday are fine. Can the driver use the side entrance? The front is locked until 10."),
            (A, "Done. I've added a note for the driver about the side entrance and moved you to the morning route from the 1st."),
            (C, "Perfect, thank you."),
        ]),
        new("{product} discontinued: what can we order instead?", TicketPriority.Normal,
        [
            (C, "We've just been told {product} is discontinued. It's one of our best sellers, so we need something close to it.\n\nIdeally it would have a similar shelf life and price. We'd also prefer a supplier we already buy from, as it keeps our paperwork down.\n\nWhat would you suggest?"),
            (A, "Hi {first},\n\nI've looked at what you buy from us, and there are two options I'd suggest:\n\n- {product2}: the closest in taste and shelf life, about 8% more per unit.\n- Our own-label equivalent: a little cheaper, and available in the same case sizes.\n\nI can put a sample case of each on your next delivery so you can try them before you decide."),
            (C, "Samples would be great, thanks."),
            (C, "We tried both. We'll go with {product2}. Can you switch our standing order?"),
            (A, "Done. Your standing order has {product2} in place of {product} from the next delivery."),
        ]),
        new("New billing address", TicketPriority.Low,
        [
            (C, "We've moved our office. Please update our billing address to the one in my signature from next month's invoices onwards. The delivery address stays the same.\n\n{contact}\n{company}"),
            (A, "Thanks {first}, I've updated the billing address. Invoices from the 1st will go to the new address, and deliveries are unchanged."),
            (C, "Thank you."),
        ]),
        new("Price rise on {product}", TicketPriority.Normal,
        [
            (C, "The price of {product} went up by almost 12% on our last invoice. We weren't told about this. What's going on?"),
            (A, "Hi {first},\n\nThe supplier raised their price at the start of the month, mostly because of packaging and freight costs. We sent a price change notice to your account email three weeks ago, but I can see it went to an address that isn't monitored any more. I've changed it to yours.\n\nWe've kept our margin flat, so the whole increase is the supplier's. If volume helps, we can offer 4% off orders of ten cases or more."),
            (C, "Thanks for explaining. We'll stay at our usual volume for now."),
            (A, "Understood. I'll let you know if the supplier's price comes back down."),
        ]),
        new("Set up a standing weekly order", TicketPriority.Low,
        [
            (C, "We order more or less the same thing every week. Can we set up a standing order so we don't have to place it each time?"),
            (A, "Of course. Reply with the products and quantities you want each week, and the delivery day, and I'll set it up. You can still change or skip a week up to 48 hours before delivery."),
            (C, "{qty} of {product} and 10 of {product2}, delivered on Thursdays."),
            (A, "That's set up from next Thursday. You'll get a reminder email two days before each delivery, with a link to change it."),
            (C, "Brilliant, thanks."),
        ]),
        new("Can't log in to the ordering portal", TicketPriority.High,
        [
            (C, "None of us can log in to the ordering portal since this morning. It says our account is locked."),
            (A, "Hi {first}, the account was locked after several failed password attempts. I've unlocked it and sent a password reset link to your email. The link is valid for an hour."),
            (C, "That worked, thanks. We think someone typed the old password on the shared tablet."),
            (A, "That would do it. If you'd like separate logins for each member of staff, I can set them up, so one person getting it wrong doesn't lock everyone out."),
            (C, "Yes please, for three people. I'll send their names."),
            (A, "Logins created; each person will get their own invitation email."),
        ]),
        new("Request for 60-day payment terms", TicketPriority.Normal,
        [
            (C, "Hello,\n\nWe've been ordering from you for several years, and our volumes have grown a lot over the past twelve months. Our other suppliers give us 60 days, and moving you from 30 to 60 days would make our cash flow much easier to manage.\n\nIs that something you'd consider?\n\nBest regards,\n{contact}"),
            (A, "Hi {first}, thanks for asking. Payment terms are set by our credit team, so I've passed your request on with a note about your order history. They usually reply within a week."),
            (A, "The credit team can offer 45 days from next month, and will review it again in six months. Would that work for you?"),
            (C, "45 is better than 30. We'll take it."),
            (A, "Great. Your next statement will show the new terms."),
        ]),
        new("Certificate of origin missing for order #{order}", TicketPriority.Urgent,
        [
            (C, "Customs are holding part of order #{order} because there's no certificate of origin for the {product}. Can you send one urgently?"),
            (A, "Hi {first}, I'm sorry, that should have been in the shipping pack. I've asked the supplier for a copy and I'll forward it as soon as it arrives."),
            (C, "Customs say they'll release it if they have it by Thursday. After that it goes back."),
            (A, "The supplier has sent the certificate. I've emailed it to you and to the customs broker named on the shipping documents, and the broker has confirmed they have it."),
            (C, "Released this afternoon. Thanks."),
        ]),
    ];

    // ───────────── The order-sync incident: a few hundred messages ─────────────

    static Ticket IncidentTicket(Customer customer, SeedOrder? order, DateTime today, Random r)
    {
        var contact = customer.Contact ?? customer.Company;
        var thread = new ThreadBuilder();
        const string Sync = "Order sync";
        var engineer = "Robert King";
        var batch = 88_400;
        var heldPos = new List<string>();
        string Po() => $"SAL-{r.Next(4400, 4499)}-{r.Next(100, 999):0000}";

        thread.Add(MessageAuthor.Customer, contact, TimeSpan.Zero,
            "Hi,\n\nSince yesterday evening we're seeing duplicate orders from you. Our stores send their replenishment orders through the EDI link as usual, but several of them have now received the same delivery twice, and we've been invoiced for both.\n\n"
            + "So far we've found:\n\n- Store 12: two identical deliveries of dairy on Tuesday\n- Store 31: three copies of the same beverages order\n- Store 7: one duplicate, which they refused at the door\n\n"
            + "We haven't changed anything on our side as far as I know. Can you stop this before tonight's batch? Our stores are running out of space in the chillers.\n\nJose Pavarotti\nSupply chain, Save-a-lot Markets");
        thread.Add(MessageAuthor.Agent, "Laura Callahan", TimeSpan.FromMinutes(18),
            "Hi Jose, thanks for the detail, that helps a lot. I've raised this as a P1 incident and our integration engineer, Robert King, is joining the ticket.\n\n"
            + "As a first step we're putting your EDI feed into quarantine: orders still come in, but anything that looks like a duplicate is held for a person to review instead of going to the warehouse. Your stores won't get any more double deliveries while we investigate.");
        thread.Add(MessageAuthor.System, Sync, TimeSpan.FromMinutes(2),
            "Incident INC-2417 opened. SAVEA-EDI switched to quarantine mode: possible duplicates are held for review before import.");
        thread.Add(MessageAuthor.Agent, engineer, TimeSpan.FromMinutes(40),
            "Robert here. First look at the logs: the duplicates aren't coming from your stores sending twice. Your EDI gateway is retrying batches it thinks have failed, and our endpoint is accepting the retry as a new batch.\n\n"
            + "Why it thinks they failed is the next question. I'll post what I find here as I go, so this thread will get long.");

        while (thread.Count < 414)
        {
            var roll = r.Next(100);
            var n = r.Next(4, 40);
            if (thread.Count % 60 == 0)
            {
                thread.Add(MessageAuthor.Agent, engineer, Minutes(r, 20, 60), IncidentSummary(thread.Count, batch, heldPos.Count, r));
            }
            else if (roll < 52)
            {
                batch++;
                var held = r.Next(10) < 3 ? r.Next(1, 4) : 0;
                if (held == 0)
                {
                    thread.Add(MessageAuthor.System, Sync, Minutes(r, 4, 40), $"Batch {batch} from SAVEA-EDI: {n} orders received, {n} imported.");
                }
                else
                {
                    var pos = Enumerable.Range(0, held).Select(_ => Po()).ToList();
                    heldPos.AddRange(pos);
                    thread.Add(MessageAuthor.System, Sync, Minutes(r, 4, 40),
                        $"Batch {batch} from SAVEA-EDI: {n} orders received, {n - held} imported, {held} held as possible duplicates ({string.Join(", ", pos)}).");
                }
            }
            else if (roll < 62)
            {
                thread.Add(MessageAuthor.System, Sync, Minutes(r, 1, 6), r.Next(4) switch
                {
                    0 => $"Batch {batch}: retry {r.Next(1, 5)} of 5 after the gateway timed out waiting for an acknowledgement (30 s).",
                    1 => $"Batch {batch}, line {r.Next(2, 60)}: quantity '{r.Next(2, 40)},0' isn't a whole number. Line rejected.",
                    2 => $"Queue depth {r.Next(0, 30)}, oldest message {r.Next(1, 25)} min.",
                    _ => $"Acknowledgement for batch {batch} sent after {r.Next(26, 48)}.{r.Next(0, 9)} s.",
                });
            }
            else if (roll < 72 && heldPos.Count > 0)
            {
                var po = heldPos[r.Next(heldPos.Count)];
                heldPos.Remove(po);
                var reviewer = Agents[r.Next(2)];
                thread.Add(MessageAuthor.System, Sync, Minutes(r, 3, 30), r.Next(3) == 0
                    ? $"Held order {po} released by {reviewer} after review: not a duplicate."
                    : $"Held order {po} cancelled by {reviewer}: duplicate of an order imported earlier.");
            }
            else if (roll < 84)
            {
                thread.Add(MessageAuthor.Agent, r.Next(3) == 0 ? "Laura Callahan" : engineer, Minutes(r, 15, 90), Pick(IncidentNotes, r).Replace("{batch}", batch.ToString()).Replace("{po}", Po()));
            }
            else if (roll < 93)
            {
                thread.Add(MessageAuthor.Customer, contact, Minutes(r, 30, 120), Pick(IncidentCustomer, r).Replace("{store}", r.Next(2, 48).ToString()));
            }
            else
            {
                thread.Add(MessageAuthor.Agent, engineer, Minutes(r, 40, 180), Pick(IncidentDeepDives, r).Replace("{batch}", batch.ToString()));
            }
        }

        thread.Add(MessageAuthor.Agent, engineer, Minutes(r, 30, 60),
            "Root cause confirmed. Our acknowledgement step waits for the warehouse to reserve stock before replying to your gateway. Since last week's warehouse update, that reservation takes longer for large batches, so we were replying after your gateway's 30-second timeout. The gateway then resent the batch, and because the resend has a new batch number we imported it again.\n\n"
            + "The fix has two parts:\n\n- We now acknowledge as soon as a batch is stored, and reserve stock afterwards.\n- We check every incoming order against the store's PO number, so a resent order is recognised even if the batch number changes.\n\n"
            + "Both are tested and go live in tonight's release window at 22:00.");
        thread.Add(MessageAuthor.Customer, contact, Minutes(r, 20, 50), "Thanks Robert, that makes sense. We'll keep an eye on the morning orders and let you know.");
        thread.Add(MessageAuthor.System, Sync, Minutes(r, 300, 400), "Release 2026.10.2 deployed. SAVEA-EDI acknowledgements now sent on receipt; PO-level duplicate check enabled.");
        thread.Add(MessageAuthor.System, Sync, Minutes(r, 200, 260), $"Batch {++batch} from SAVEA-EDI: 31 orders received, 31 imported. Acknowledged in 0.4 s.");
        thread.Add(MessageAuthor.Customer, contact, Minutes(r, 40, 90), "Morning orders all came through once each. Can you leave quarantine on until the weekend, just in case?");
        thread.Add(MessageAuthor.Agent, "Laura Callahan", Minutes(r, 10, 30), "Will do, Jose. Quarantine stays on until Monday, and we'll review the held orders with you before we close the incident.");

        var messages = thread.EndingAt(today.AddHours(7).AddMinutes(40));
        return new Ticket
        {
            Subject = "Order sync: duplicate orders from your EDI feed",
            CustomerId = customer.Id,
            OrderId = order?.Id,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Urgent,
            OpenedOn = messages[0].SentOn,
            LastActivityOn = messages[^1].SentOn,
            ReadUpTo = 311,
            Messages = messages,
        };
    }

    static string IncidentSummary(int count, int batch, int held, Random r) =>
        $"Incident summary (update {count / 60 + 1})\n\n"
        + $"Where we are: the duplicate check is holding orders as expected, and no duplicate has reached a store since quarantine started. {held} orders are waiting for review right now. The last batch processed was {batch}.\n\n"
        + "What we know:\n\n"
        + "- Duplicates only happen when a batch takes longer than 30 seconds to acknowledge.\n"
        + "- Slow acknowledgements line up with large batches (more than 25 orders) going through stock reservation.\n"
        + $"- The gateway's retries make up about {r.Next(4, 9)}% of all batches since the start of the incident.\n\n"
        + "Next steps: keep reviewing held orders twice a day, and finish testing the change that acknowledges batches on receipt. I'll post again when that's ready to deploy.";

    static readonly string[] IncidentNotes =
    [
        "Reviewed the held orders from the last few hours. All but one were genuine duplicates; the other was a store ordering the same thing twice on purpose. Released it.",
        "Batch {batch} took 41 seconds to acknowledge. That's the third slow one today, all over 25 orders.",
        "Jose, could you check whether your gateway's timeout is configurable? If it can go from 30 to 90 seconds, that would stop most retries while we work on the real fix.",
        "Quick note for whoever is on shift tonight: the held-orders view is sorted oldest first. Please work from the top.",
        "Found the retry setting in the gateway logs you sent: 5 attempts, 30 seconds apart. That matches what we're seeing.",
        "A store queried a missing order this morning ({po}). It was held as a duplicate but wasn't one. Released it and asked the warehouse to put it on today's run.",
        "I've added an alert so we're paged if any batch takes more than 25 seconds to acknowledge.",
        "The warehouse team confirmed their update last Tuesday changed how stock reservation works for multi-pallet orders. That's our main suspect now.",
        "Ran last week's batches through the test environment with the new reservation code. Acknowledgement times went from about 3 seconds to between 20 and 45. That's it.",
        "Credit notes for the duplicate deliveries to stores 12 and 31 have been raised. Accounts will send them today.",
        "Nothing unusual overnight. Two held, both duplicates, both cancelled.",
        "Handing over to Laura for the afternoon; I'm in a release review until 4.",
    ];

    static readonly string[] IncidentCustomer =
    [
        "Store {store} says they're missing an order from this morning. Can you check if it's in the held list?",
        "Thanks for the update.",
        "Our IT team says the gateway timeout is fixed at 30 seconds in the version we run. Upgrading is a two-month project, so that's not an option right now.",
        "No duplicates reported by any store today. Good news.",
        "Store {store} received a delivery twice again. Was that before quarantine started?",
        "Our finance team has asked for a list of all the duplicate invoices so they can match them against your credit notes. Is that something you can send?",
        "Is there anything we should do differently on our side in the meantime?",
        "Quick one: will the held orders still make the afternoon delivery if you release them before 11?",
        "Thanks Robert. That explanation helped our IT team a lot.",
        "We're opening two new stores next month, which will add about 15% to our order volume. Is that going to make this worse?",
    ];

    static readonly string[] IncidentDeepDives =
    [
        "A longer update on where the time goes in a slow batch, since a few people have asked.\n\n"
            + "When a batch arrives, we validate it, store it, and then ask the warehouse system to reserve stock for every line before we send the acknowledgement back to your gateway. For a typical batch of 10 orders that takes 2–3 seconds in total.\n\n"
            + "Since the warehouse update, reserving stock for a pallet-sized line means checking every bay that holds that product, one at a time. Batch {batch} had 31 orders and 140 lines, and reservation alone took 38 seconds.\n\n"
            + "So the acknowledgement arrives after your gateway has given up and resent the batch. Each resend gets a new batch number, which is why our duplicate check (which works on batch numbers) didn't catch it.",
        "On why we can't just make the warehouse faster:\n\n"
            + "The warehouse change was made for good reasons. It stops two pickers being sent to the same bay, which was causing short picks. Rolling it back would bring that problem back for every customer.\n\n"
            + "The better fix is to stop making your gateway wait for something it doesn't need. It only needs to know we've received the batch safely. Reserving stock can happen a moment later.",
        "Testing plan for the fix, for the record:\n\n"
            + "- Replay a week of Save-a-lot batches, including the slow ones, against the new acknowledgement code.\n"
            + "- Check every batch is acknowledged in under a second.\n"
            + "- Resend 50 batches with new batch numbers and confirm the PO-level check catches every one.\n"
            + "- Run the warehouse reservation afterwards and confirm stock levels match the old process exactly.\n\n"
            + "If all four pass, it goes into the next release window.",
        "I looked at the held-orders numbers over the past few days to see whether quarantine is catching everything.\n\n"
            + "Of the orders held so far, about 9 in 10 were real duplicates. The rest were stores legitimately ordering the same products twice in one day, which the check can't tell apart from a resend. That's why a person reviews them rather than the system cancelling them automatically.\n\n"
            + "Once the PO-level check is live, legitimate repeat orders will have different PO numbers and won't be held at all.",
    ];

    // ───────────── Cold chain investigation ─────────────

    static Ticket ColdChainTicket(Customer customer, DateTime today, Random r)
    {
        var contact = customer.Contact ?? customer.Company;
        var thread = new ThreadBuilder();
        const string Telemetry = "Van telemetry";
        string[] stops = ["Old Town Market", "Rattlesnake Canyon (main)", "Mesa Drive", "Rattlesnake Canyon (north)", "Plaza Mayor"];

        thread.Add(MessageAuthor.Customer, contact, TimeSpan.Zero,
            "Hello,\n\nOur goods-in team has recorded chilled deliveries from you arriving above 5°C three times this week, all on the morning run. The dairy was at 7–8°C on arrival, which is over our limit, so we had to reject it.\n\n"
            + "We need to understand what's happening before our food safety audit at the end of the month. Can you share the temperature records for those deliveries?\n\nPaula Wilson\nRattlesnake Canyon Grocery");
        thread.Add(MessageAuthor.Agent, "Margaret Peacock", TimeSpan.FromHours(2),
            "Hi Paula, I manage our delivery fleet. Your morning deliveries are on route 7, van NW-07. Every chilled van logs its temperature every few minutes, so I've connected this ticket to the van's alerts: anything outside the limits will be posted here automatically, and I'll add the history for this week below.");

        var day = 0;
        while (thread.Count < 236)
        {
            var roll = r.Next(100);
            if (roll < 55)
            {
                var temp = 5 + r.Next(1, 40) / 10.0;
                thread.Add(MessageAuthor.System, Telemetry, Minutes(r, 6, 70),
                    $"NW-07 · chiller at {temp:0.0}°C for {r.Next(3, 22)} min (limit 5.0°C) · {Pick(stops, r)}{(r.Next(3) == 0 ? $" · rear door open {r.Next(2, 9)} min" : "")}");
            }
            else if (roll < 63)
            {
                day++;
                thread.Add(MessageAuthor.System, Telemetry, Minutes(r, 300, 700),
                    $"NW-07 · route 7 completed · {r.Next(9, 15)} stops · average chiller {3 + r.Next(0, 15) / 10.0:0.0}°C · {r.Next(0, 6)} alerts");
            }
            else if (roll < 75)
            {
                thread.Add(MessageAuthor.System, "Route 7 driver", Minutes(r, 10, 60), Pick(DriverNotes, r));
            }
            else if (roll < 88)
            {
                thread.Add(MessageAuthor.Agent, "Margaret Peacock", Minutes(r, 30, 200), Pick(ColdChainNotes, r));
            }
            else if (roll < 96)
            {
                thread.Add(MessageAuthor.Customer, contact, Minutes(r, 60, 300), Pick(ColdChainCustomer, r));
            }
            else
            {
                thread.Add(MessageAuthor.Agent, "Margaret Peacock", Minutes(r, 60, 240), Pick(ColdChainReports, r).Replace("{day}", (day + 1).ToString()));
            }
        }

        thread.Add(MessageAuthor.Agent, "Margaret Peacock", Minutes(r, 60, 120),
            "Paula, the replacement door seals for NW-07 arrive on Thursday. Until then we've swapped the order of the stops so your stores are first off the van, before the doors have been opened anywhere else. I'd like to keep this ticket open for another two weeks of readings before we call it fixed.");
        thread.Add(MessageAuthor.Customer, contact, Minutes(r, 60, 180), "That works for us. Can you send the full temperature log for the audit when the two weeks are up?");

        var messages = thread.EndingAt(today.AddDays(-1).AddHours(16).AddMinutes(5));
        return new Ticket
        {
            Subject = "Chilled deliveries arriving above 5°C (route 7)",
            CustomerId = customer.Id,
            Status = TicketStatus.Open,
            Priority = TicketPriority.High,
            OpenedOn = messages[0].SentOn,
            LastActivityOn = messages[^1].SentOn,
            ReadUpTo = 181,
            Messages = messages,
        };
    }

    static readonly string[] DriverNotes =
    [
        "Loading bay at Old Town was blocked, waited 12 min with the engine off. Chiller off while the engine's off.",
        "Rear door seal is loose on the left side again. Taped it for now.",
        "All drops on time today.",
        "Customer at Mesa Drive asked us to wait while they cleared space in the cold room. Doors open about 8 minutes.",
        "Chiller fan making a rattling noise from about 9am.",
        "Took the bypass because of roadworks, added 15 minutes to the route.",
        "Van pre-cooled for 30 min before loading as asked.",
    ];

    static readonly string[] ColdChainNotes =
    [
        "The alerts cluster between the second and fourth stops, which is when the van has been opened most often. That points at the doors rather than the chiller unit.",
        "We've asked drivers to pre-cool the van for 30 minutes before loading on route 7. Let's see what that does to the morning readings.",
        "Today's readings look better: only two short excursions, both at the stop where the driver had to wait with the doors open.",
        "The chiller unit was serviced last month and the engineer's report says it's working within spec. I've asked them to check it again anyway.",
        "Paula, the readings on the log are taken at the chiller outlet. The core temperature of the product will rise more slowly, but your team is right to go by the probe at goods-in.",
        "Booked NW-07 in for a door seal inspection on Monday. A spare van covers route 7 that day.",
        "Fleet maintenance found the left rear door seal is perished along the bottom edge. New seals are on order.",
        "Comparing NW-07 with NW-05, which runs a similar route: NW-05 had no excursions this week. Same chiller model, newer door seals.",
    ];

    static readonly string[] ColdChainCustomer =
    [
        "Another delivery rejected this morning, dairy at 7.4°C. Can someone call me?",
        "Thanks Margaret, this is really useful.",
        "Today's delivery was at 4.1°C on arrival. Much better.",
        "Our auditor will want to see what you've changed and the readings before and after. Can you put that together when you're done?",
        "Can the chilled goods be loaded last, so they're nearest the doors and come off first?",
        "No problems this week from our side. Fingers crossed.",
    ];

    static readonly string[] ColdChainReports =
    [
        "Day {day} report for route 7.\n\nThe van left the depot at 3.2°C after pre-cooling. The first three stops were fine. At the fourth stop the driver waited 11 minutes for the loading bay, with the rear door open for most of that time, and the chiller climbed to 7.8°C. It took 25 minutes to recover.\n\n"
            + "Your delivery was the stop after that, so the product was probably still above 5°C when it arrived. This is the same pattern as Tuesday and Wednesday.",
        "Summary of what we've changed so far:\n\n- Vans on route 7 pre-cool for 30 minutes before loading.\n- Chilled goods are loaded last, nearest the doors.\n- Drivers close the rear door between trips to the store instead of leaving it open.\n- NW-07's door seals are being replaced.\n\n"
            + "Since the first three changes went in, excursions on your deliveries are down from most days to about one a week.",
        "I've pulled the log for day {day} together for your auditor. It has every reading for your deliveries, the alerts, and what the driver did about each one. I'll send the full file at the end of the monitoring period, as you asked.",
    ];

    // ───────────── Contract negotiation: long emails ─────────────

    static Ticket ContractTicket(Customer customer, DateTime today, Random r)
    {
        var contact = customer.Contact ?? customer.Company;
        var thread = new ThreadBuilder();
        const string AccountManager = "Andrew Fuller";

        thread.Add(MessageAuthor.Customer, contact, TimeSpan.Zero,
            "Dear Andrew,\n\nAs discussed at our meeting in Graz, I'm writing to start the review of our supply agreement for next year. Ernst Handel's volumes with you grew by about a fifth this year, and we expect similar growth next year as we open two new stores in Salzburg.\n\n"
            + "We'd like to discuss three things: pricing for our top 30 products, delivery windows for the new stores, and payment terms. I'll send our volume forecast separately.\n\nMit freundlichen Grüßen,\nRoland Mendel\nPurchasing director, Ernst Handel");

        while (thread.Count < 128)
        {
            var fromCustomer = thread.Count % 2 == 0;
            if (r.Next(6) == 0)
            {
                thread.Add(fromCustomer ? MessageAuthor.Customer : MessageAuthor.Agent, fromCustomer ? contact : AccountManager, Minutes(r, 60, 600),
                    Pick(fromCustomer ? ContractShortCustomer : ContractShortAgent, r));
                continue;
            }

            var paragraphs = Enumerable.Range(0, r.Next(1, 5)).Select(_ => Pick(fromCustomer ? ContractCustomerParagraphs : ContractAgentParagraphs, r)).Distinct();
            var body = (fromCustomer ? "Dear Andrew,\n\n" : "Dear Roland,\n\n")
                + string.Join("\n\n", paragraphs)
                + (fromCustomer ? "\n\nBest regards,\nRoland" : "\n\nKind regards,\nAndrew Fuller\nVice President, Sales");
            thread.Add(fromCustomer ? MessageAuthor.Customer : MessageAuthor.Agent, fromCustomer ? contact : AccountManager, Minutes(r, 120, 2000), body);
        }

        thread.Add(MessageAuthor.Customer, contact, Minutes(r, 600, 1200),
            "Dear Andrew,\n\nOur board met yesterday and approved the agreement on the terms in your last message, with one change: we'd like the price review to happen every six months rather than once a year.\n\nIf you can agree to that, please send the final version for signature.\n\nBest regards,\nRoland");
        thread.Add(MessageAuthor.Customer, contact, Minutes(r, 30, 90), "One more thing: could the new Salzburg stores start on the agreement from the day they open, rather than from January?");

        var messages = thread.EndingAt(today.AddDays(-2).AddHours(11).AddMinutes(30));
        return new Ticket
        {
            Subject = "Supply agreement for next year: pricing and volumes",
            CustomerId = customer.Id,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            OpenedOn = messages[0].SentOn,
            LastActivityOn = messages[^1].SentOn,
            ReadUpTo = messages.Count - 2,
            Messages = messages,
        };
    }

    static readonly string[] ContractCustomerParagraphs =
    [
        "Thank you for the revised price list. The changes on dairy and dry goods are acceptable, but the increase on beverages is still higher than we can accept. Our competitors are paying less for the same products, and our customers notice the difference on the shelf.",
        "On delivery windows: the new Salzburg stores have a shared loading bay with the shopping centre, which is only available before 8am. We'd need deliveries to arrive between 5:30 and 7:30 on weekdays.",
        "Our finance director has asked me to raise payment terms again. We currently pay at 30 days. With the volumes we're now committing to, we think 45 days is reasonable.",
        "I've attached our forecast by product group. The biggest growth is in cheese and cured meats, where we expect volumes to rise by around 30% once the new stores are open.",
        "We would also like to talk about the rebate. At the moment it's paid annually, which makes it hard to plan. A quarterly rebate would suit us much better, even if the percentage stayed the same.",
        "Our sustainability team has asked whether you can deliver in reusable crates rather than cardboard for chilled products. We'd be happy to pay a deposit on the crates.",
        "I've checked the figures in your proposal against our records, and there's a difference in the volume of Gorgonzola Telino we bought this year. Our records show around 1,800 kg, not 1,200 kg. Could you check?",
        "We're happy with service levels in general. The one complaint from our stores is late deliveries on Mondays, which happens two or three times a month.",
        "To be clear, we would like to continue working with Northwind. But we need a deal that our board can see is competitive, and the beverage prices are the sticking point.",
        "If we could agree a price for the year with a review halfway through, rather than prices that change every quarter, that would make our planning much easier.",
    ];

    static readonly string[] ContractAgentParagraphs =
    [
        "Thank you for your detailed reply. I've reviewed the beverage pricing with our category manager. We can bring the increase on beers and soft drinks down to 3%, which matches what our suppliers are charging us. We can't go lower on wines and spirits, as those prices are set by the producers.",
        "On delivery windows, our Salzburg route can be scheduled to arrive between 5:30 and 7:30. That means an earlier start for the drivers, so there is a small surcharge per delivery, which we'd waive for the first three months while the new stores settle in.",
        "You're right about the Gorgonzola figure: our report excluded orders shipped from the Italian warehouse. With those included, it's 1,790 kg. I've corrected the proposal.",
        "A quarterly rebate is something we can do. The percentage would stay the same, paid within 30 days of the end of each quarter.",
        "On payment terms, our credit team is comfortable with 45 days given your payment history, provided the agreement runs for at least two years.",
        "Reusable crates are something we already do for two other customers in Austria. There's a deposit of 4 euros per crate, refunded when they're returned. I'll send you the details.",
        "I'm sorry to hear about the Monday deliveries. The Monday route starts from our Munich depot, which is affected by weekend traffic restrictions for lorries. We're looking at moving your Monday delivery to the Linz depot, which should fix that.",
        "I've attached the updated proposal with all the changes we've agreed so far, highlighted in yellow. The only open points are the price review schedule and the start date for the new stores.",
        "Fixed prices for twelve months with a review at six months would work for us, as long as the review can take account of large changes in supplier prices.",
        "I understand the board needs to see a competitive offer. I'd suggest we meet in Graz next week to go through the beverage list line by line; I'm sure we can find a way to close the gap on the products that matter most to your stores.",
    ];

    static readonly string[] ContractShortCustomer =
    [
        "Thanks Andrew, I'll take this to our board on Thursday.",
        "Received, thank you. I'll reply in detail tomorrow.",
        "Could we have a call on Monday at 10 to go through this?",
        "Just checking you received my last message?",
    ];

    static readonly string[] ContractShortAgent =
    [
        "Thanks Roland, I'll come back to you by Friday.",
        "Monday at 10 works for me. I'll send an invitation.",
        "Received, thank you. I'm checking the figures with our finance team.",
        "Apologies for the slow reply, I've been travelling. Full response to follow tomorrow.",
    ];

    // ───────────── Helpers ─────────────

    sealed class ThreadBuilder
    {
        readonly List<TicketMessage> messages = [];
        DateTime clock = new(2000, 1, 1);

        public int Count => messages.Count;

        public void Add(MessageAuthor kind, string author, TimeSpan after, string body)
        {
            clock += after;
            messages.Add(new TicketMessage { Number = messages.Count + 1, AuthorKind = kind, Author = author, SentOn = clock, Body = body });
        }

        /// <summary>Shifts the whole thread so its last message was sent at <paramref name="end"/>.</summary>
        public List<TicketMessage> EndingAt(DateTime end)
        {
            var shift = end - clock;
            foreach (var message in messages)
            {
                message.SentOn += shift;
            }
            return messages;
        }
    }

    sealed record SeedOrder(int Id, string CustomerId, DateTime PlacedOn, string Product);

    static TimeSpan Minutes(Random r, int min, int max) => TimeSpan.FromMinutes(r.Next(min, max));

    static T Pick<T>(T[] items, Random r) => items[r.Next(items.Length)];

    static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    static string Fill(string template, Dictionary<string, string> tokens)
        => tokens.Aggregate(template, (text, token) => text.Replace(token.Key, token.Value));
}
