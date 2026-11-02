/**
 * Public identity for the website.
 * The repository name remains The Wedding Bank.
 * No public name, mark, address, or history had been decided, so the
 * site uses one consistent institutional identity: Ambre & Jules.
 * The address and correspondence address are fictional.
 * Telephone numbers use a Swiss shape with one extra digit, so they cannot be dialled.
 */

export const bank = {
  name: 'Ambre & Jules',
  descriptor: 'Private bankers',
  established: 1894,
  geneva: {
    city: 'Geneva',
    lines: ['14 Rue de la Cité', '1204 Geneva', 'Switzerland'],
    telephone: '+41 22 319 48 207',
  },
  zurich: {
    city: 'Zurich',
    lines: ['6 Stadelhoferstrasse', '8001 Zurich', 'Switzerland'],
    telephone: '+41 44 380 19 403',
  },
  correspondence: 'correspondence@ambre-jules.ch',
} as const

export const nav = [
  { href: '/about', label: 'About us' },
  { href: '/services', label: 'Our services' },
  { href: '/expertise', label: 'Expertise' },
  { href: '/insights', label: 'Insights' },
  { href: '/contact', label: 'Contact' },
] as const

export const services = [
  {
    title: 'Private wealth management',
    summary: 'A single relationship for families whose affairs cross generations, currencies, and jurisdictions.',
    detail:
      'Each mandate is led by a named relationship manager and supported by the investment office. We begin with the family’s objectives, liabilities, and obligations, and we organise the balance sheet around those facts rather than around a product list.',
  },
  {
    title: 'Investment advisory',
    summary: 'Independent advice for clients who retain discretion over implementation.',
    detail:
      'We prepare written recommendations, explain the reasoning, and leave the decision with the client. Advice covers asset allocation, manager selection, and the role of liquidity. We do not publish forecasts, and we do not present past market movements as a promise.',
  },
  {
    title: 'Portfolio management',
    summary: 'Discretionary mandates governed by a written investment policy.',
    detail:
      'Where a client delegates implementation, the portfolio is managed against an agreed policy: objective, risk, liquidity, exclusions, and reporting. Changes to that policy are made in writing. Custody remains with established institutions, separate from the partnership.',
  },
  {
    title: 'Wealth planning',
    summary: 'Coordination of the financial, legal, and family questions that sit around capital.',
    detail:
      'We work with the family’s existing counsel. Our role is to keep the financial picture coherent: ownership, income needs, philanthropic commitments, and the practical consequences of residence. We do not provide legal, tax, or accounting advice.',
  },
  {
    title: 'Estate and succession planning',
    summary: 'A calm framework for the transfer of responsibility, not only of assets.',
    detail:
      'Succession is treated as a governance question. We help families document intent, prepare the next generation for the information they will need, and align investment policy with the timetable they choose. External notaries and counsel remain responsible for the instruments.',
  },
] as const

export const departments = [
  {
    name: 'Investment office',
    mandate:
      'Sets house views on allocation, reviews external managers, and prepares the written policy for each mandate. The office does not sell products of the partnership, because the partnership does not manufacture them.',
  },
  {
    name: 'Wealth planning',
    mandate:
      'Coordinates the information required for long-term decisions and keeps a single record of the family’s stated objectives. Specialist advice is commissioned from independent counsel.',
  },
  {
    name: 'Family governance',
    mandate:
      'Assists families who wish to record how decisions are made, who is informed, and how the next generation is introduced to the mandate. The work is documentary and procedural.',
  },
  {
    name: 'Client office',
    mandate:
      'Receives new enquiries, arranges meetings in Geneva or Zurich, and maintains the correspondence of existing relationships. Access to reporting is granted only after a relationship has been accepted.',
  },
  {
    name: 'Custody oversight',
    mandate:
      'Monitors the appointed custodians, reconciles reporting, and keeps the operational relationship separate from investment decisions. Assets are not held on the partnership’s own balance sheet.',
  },
] as const

export const values = [
  {
    title: 'Independence',
    text: 'The partnership is privately held. Advice is not organised around a house product.',
  },
  {
    title: 'Discretion',
    text: 'Client affairs are discussed only with the people the client has authorised.',
  },
  {
    title: 'Continuity',
    text: 'Mandates are designed to be readable by the next generation of both the family and the partnership.',
  },
  {
    title: 'Prudence',
    text: 'We prefer a clear policy and a patient implementation to a frequent change of view.',
  },
] as const

export const history = [
  {
    year: '1894',
    text: 'The partnership is established in Geneva to look after the capital of a small number of entrepreneurial families.',
  },
  {
    year: '1928',
    text: 'The articles are revised to keep the partnership private and to prohibit a public listing.',
  },
  {
    year: '1958',
    text: 'A Zurich office is opened for clients whose business and family life had moved between the two cities.',
  },
  {
    year: '1986',
    text: 'Custody is placed entirely with third-party institutions. The partnership retains advice, oversight, and the client relationship.',
  },
  {
    year: '2012',
    text: 'The investment office is given a written mandate, separate from relationship management, so that policy and conversation are not the same task.',
  },
] as const

export type Article = {
  slug: string
  date: string
  category: string
  title: string
  standfirst: string
  paragraphs: readonly string[]
}

export const articles: readonly Article[] = [
  {
    slug: 'patience-and-the-private-mandate',
    date: '12 March 2026',
    category: 'Investment office',
    title: 'Patience and the private mandate',
    standfirst:
      'A private portfolio is a set of obligations stretched over time. The useful question is whether the policy still fits those obligations.',
    paragraphs: [
      'Families rarely ask us to predict the next quarter. They ask whether the capital will still be able to meet a known set of commitments: a household, a business interest, a gift, or the arrival of another generation.',
      'A mandate that changes its character with every market commentary becomes difficult to explain later. We therefore write the objective, the tolerance for loss, and the need for cash before we discuss instruments.',
      'Patience is not inactivity. It is the decision to alter the portfolio when the family’s circumstances change, or when an investment no longer serves the written policy, rather than when a headline changes.',
      'This note is an institutional comment. It is not a recommendation to buy or sell any security, and it describes no expected return.',
    ],
  },
  {
    slug: 'succession-and-the-family-balance-sheet',
    date: '28 January 2026',
    category: 'Wealth planning',
    title: 'Succession and the family balance sheet',
    standfirst:
      'The transfer of capital is easier to discuss once the family can see the balance sheet in ordinary language.',
    paragraphs: [
      'Succession conversations fail when they begin with instruments. They proceed when the family can name what the capital is for, who depends on it, and which decisions must remain collective.',
      'We ask for a plain inventory: operating businesses, residences, financial assets, and the obligations attached to each. The inventory is a working paper. It is not a valuation and it is not a legal scheme.',
      'The next generation does not need every document on the first afternoon. They need to know where the policy is written, who the advisers are, and how a question reaches the partnership.',
      'Legal instruments remain the work of the family’s notaries and counsel. Our contribution is to keep the financial description accurate enough for that work to begin.',
    ],
  },
  {
    slug: 'currency-and-international-families',
    date: '4 November 2025',
    category: 'Investment office',
    title: 'Currency and internationally mobile families',
    standfirst:
      'A family that lives in more than one currency has a liability as well as an asset. The liability deserves to be named.',
    paragraphs: [
      'School fees, staff, property, and a business can each be denominated differently from the portfolio that is expected to support them. Treating the portfolio as a single home currency hides that fact.',
      'We start by listing spending and commitments by currency over a horizon the family recognises, often five to ten years. Only then do we discuss how much of the financial capital should sit near those commitments.',
      'The remainder can be invested against the longer objective. The division is a policy choice. It is revised when residence, family composition, or the business changes.',
      'Nothing in this note is a view on the future level of any exchange rate.',
    ],
  },
  {
    slug: 'liquidity-and-the-long-mandate',
    date: '17 September 2025',
    category: 'Client office',
    title: 'Liquidity and the long mandate',
    standfirst:
      'A long investment horizon is only credible if the family can meet its near obligations without disturbing the policy.',
    paragraphs: [
      'Illiquid holdings can be appropriate for a family that understands the timetable. They are a poor answer to a payment that is already dated.',
      'We separate a reserve for known withdrawals from the capital that is invested for the longer term. The reserve is sized from the family’s own calendar, not from a general rule of thumb.',
      'When a withdrawal is planned, it is written into the reporting so that the investment office is not asked to raise cash as a surprise.',
      'This comment does not assess any private market, fund, or security. It describes how a mandate can remain orderly.',
    ],
  },
]

export const disclaimer =
  'These notes are published by Ambre & Jules as institutional commentary. They are not investment advice, an offer, or a statement of future performance.'
