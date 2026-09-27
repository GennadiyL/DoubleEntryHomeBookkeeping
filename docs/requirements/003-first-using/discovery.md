# Requirements Discovery: First using

## Status and Sources

- Feature folder: `docs/requirements/003-first-using/`.
- Version: 0.10.
- Status: Draft; non-authoritative working record.
- Recorded: 2026-09-27.
- Approval decision and provenance: None recorded.
- Separate discovery-to-business promotion decision: None recorded. BRD/TRD rework remains postponed until after brainstorming.
- Sources: User's first-use discussion, 2026-09-27; [BRD](../BRD.md); [TRD](../TRD.md); [synchronization discovery v0.21](../002-synchronization/discovery.md); repository AGENTS.md.
- Authority: Discovery only; no BRD/TRD approval or implementation authorization.

## Original Request

> [@GL Analysis](plugin://gl-analysis@git-plugins) use for analysis.
> You can find documents in the&#x20;
> s:\Gena\Local\Work\\\_Drive\Shared\DoubleEntryHomeBookkeeping\docs\requirements&#x20;
> Lets start discuss "First using feature".
>
> Let's describe (based on Android) creating
> \-- user opens app.
> \-- he filles login and password.
> \-- he chooses base currency.
> \-- he chooses template DB (in the future versions)
> \-- clicks create button.
> \-- app makes call to Azure function (internet connection is mandatory)
> \-- Azure function creates {Guid}.db (MasterDB) for this user and admin.Db for this user (with login and hash of password). Adds new device to the admin.db
> \-- App makes sync and get actual local.Db
>
> Let's describe (based on Android) adding new local db
> \-- user opens app.
> \-- he filles login and password.
> \-- clicks "open" button.
> \-- app makes call to Azure function (internet connection is mandatory)
> \-- Azure function checks data and returns actual local.Db
> \-- App checks local.Db

## Problem or Opportunity

Interpretation: initialize bookkeeping on Android, either by creating the owner's MasterDb and first LocalDb or by obtaining a LocalDb for an existing owner. Clarify onboarding without changing established synchronization rules.

## Intended Outcomes

- Creation produces a cloud MasterDb and a usable current LocalDb.
- Open obtains a current LocalDb using existing credentials.
- Creating mode requires internet for initial setup. Open/editing mode using an existing LocalDb works offline. The Open button in creating mode downloads an existing cloud database and belongs to initial setup.
- Database-template selection is deferred to future versions.

## Stakeholders

- User creating bookkeeping data or connecting a device, explicitly described by the requesting user.
- Existing ownership rule: one owner account per MasterDb and its LocalDbs.
- Deployment administrator and formal artifact approver: unspecified.

## Raw Ideas

### Mode terminology — clarified 2026-09-27

- Open mode (previously called editing mode): LocalDb exists; open and use it without internet. Offline work is the purpose of synchronization.
- Creating mode: LocalDb does not exist; internet is mandatory for setup.
- The Open button in the Create/Open setup dialog is distinct from open mode: it obtains a new LocalDb from an existing MasterDb, following the original online download flow.
- Synchronization itself still requires connectivity. Existing pending-recovery access restrictions remain separate from normal offline use.

### Startup routing — confirmed 2026-09-27

- Startup checks only whether LocalDb exists to choose the mode: present means editing mode and opens LocalDb directly; absent means creating mode with the Create/Open dialog (user clarification, 2026-09-27). Ordinary local use requires neither login nor internet.
- Otherwise, show the Create/Open dialog. This setup requires internet.
- User cannot proceed to bookkeeping without LocalDb. If initial creation/download fails and LocalDb is unavailable, setup remains incomplete; cloud MasterDb creation alone does not allow app use (user confirmation, 2026-09-27).
- First version is for the requesting user only. The Azure deployment address is hardcoded in the app; no endpoint-entry step is needed (user confirmation, 2026-09-27).
- When the setup dialog opens, check whether MasterDb exists in the deployment.
- Keep Create disabled until the check confirms that MasterDb does not exist. If MasterDb exists, Create stays disabled.
- Existing synchronization recovery/access-blocking and confirmed-expiration rules remain applicable; this normal startup flow does not override them.
- Exact no-internet/check-failure message and retry interaction remain unspecified. An unsuccessful check does not confirm absence of MasterDb.

### Create — user-stated flow

Create availability follows the startup check above.

1. User opens the Android app and enters login and password.
2. User selects base currency.
3. In a future version, user also selects a database template.
4. User clicks Create.
5. App calls an Azure Function; internet is mandatory.
6. Function creates `{Guid}.db` as MasterDb and an Admin.db for this user containing login and password hash; registers the new device in Admin.db.
7. App synchronizes and obtains the current LocalDb.

### Initial MasterDb contents — confirmed 2026-09-27

- Five required root groups: AccountGroup, CategoryGroup, ProjectGroup, CorrespondentGroup, and TemplateGroup; existing root rules apply.
- Selected base currency with rate 1.
- No accounts or transactions.
- Other currencies are added later by the user.

### Open — user-stated flow

1. User opens the Android app and enters login and password.
2. User clicks Open.
3. App calls an Azure Function; internet is mandatory.
4. Function validates credentials and registers the new device in Admin.db before downloading LocalDb (user confirmation, 2026-09-27).
5. Function returns the current LocalDb.
6. LocalDb existence determines the startup mode: present means editing mode; absent means creating mode. This is the meaning of the original “App checks local.Db” statement (user clarification, 2026-09-27).

### Existing constraints to preserve

- First release: one MasterDb per owner; multiple databases are version two.
- LocalDb cannot originate independently. Creating one requires registration and initial download from MasterDb.
- Opening an already initialized local app requires no authentication; credentials in these flows concern setup/cloud access.
- Base currency is immutable after creation; its rate is 1.
- First release supports Android and Windows desktop; this discussion describes Android.
- Successful initial registration/download starts the 90-day expiry period.
- Downloaded snapshots need local preparation; do not assume an unchanged MasterDb file is already a valid LocalDb.
- Synchronization discovery describes temporary download, validation, safe activation, acknowledgement, and restarting interrupted downloads from the beginning.
- Existing Android manual sync may start on Wi-Fi or mobile data. Applying that rule to all onboarding calls is an assumption pending confirmation.

## Alternatives Considered

Deployment alternatives considered: one owner per deployment with one Admin.db, or a shared service with separate Admin.db files per owner. User confirmed one user per deployment on 2026-09-27; existing first-release scope remains unchanged.

## Assumptions

- Open means adding this owner's existing bookkeeping database to another device or a fresh installation, not selecting another MasterDb. Confirm.

- Login/password are requested only during onboarding, consistent with existing local-use rules. Confirm any credential retention separately.
- No additional validation is inferred from the first-use existence check. Previously recorded synchronization download/activation safeguards remain separate; this clarification defines mode selection.

## Open Questions

1. Resolved, 2026-09-27: one user per deployment. Admin.db belongs to that single-user deployment; no shared multi-user service is introduced in version one. Version two is planned to use Microsoft SQL Server administration for multiple users.
2. Confirmed, 2026-09-27: open existing LocalDb directly; otherwise show online Create/Open setup, check MasterDb when the dialog opens, and keep Create disabled until absence is confirmed. Deployment address resolved, 2026-09-27: hardcoded in version one, which is for the requesting user only. Remaining: exact check-failure/retry interaction. Can remain labeled in a draft; resolve before approval.
3. Confirmed, 2026-09-27: Open registers the new device in Admin.db before downloading LocalDb. Remaining: how are reinstallations or repeated attempts identified? Can remain labeled in a draft.
4. Resolved, 2026-09-27: “App checks local.Db” means existence only, to choose creating mode (absent) or editing mode (present). The proposed additional first-use checks were not selected.
5. Resolved, 2026-09-27: initial MasterDb contains the five required root groups and selected base currency with rate 1; no accounts or transactions. Other currencies are added later.
6. Partly resolved, 2026-09-27: user cannot proceed without LocalDb; a failed initial download leaves setup incomplete even if MasterDb exists. Remaining: exact retry interaction and avoiding duplicate owner/MasterDb/device registration. The user has not selected Open versus a dedicated retry action. Can remain labeled in a draft.
7. Clarified, 2026-09-27: open/editing mode does not require internet; creating mode requires internet. No connectivity requirement applies to ordinary use of an existing LocalDb. Creating-mode network-type restrictions were not specified; do not infer a Wi-Fi-only rule. Exact setup errors/retry actions remain open.
8. Credential rules, storage for later sync, and password recovery belong partly to Administration; ownership of these details remains open. No password-hashing algorithm or credential-retention mechanism has been selected.

## Decisions and Rationale

User-stated direction on 2026-09-27: Create and Open flows above, mandatory internet, Azure Function involvement, login/password, base-currency selection on Create, cloud MasterDb and administrative password hash, device registration on Create, and local download/checking. Rationale beyond these requested outcomes was not supplied. User clarification, 2026-09-27: "one user per deployment". This confirms the earlier single-user scope: one Admin.db for that deployment and one owner; first-release one-MasterDb rule remains unchanged.

User clarification, 2026-09-27: "We should check masterdb and disable "create"". App checks for an existing MasterDb and disables Create when found. This replaces the suggested click-then-error interaction. The later clarification below settles check timing and the disabled state while checking.

User clarification, 2026-09-27:

> yes. It's not often case. If Local.db is already created user can see  opened local.db. Otherwise he should see "create"/"open" dialog. Bu it requieres internet connection

The “yes” confirms checking when the setup screen opens and keeping Create disabled until no MasterDb is confirmed. Existing LocalDb opens directly; absence leads to Create/Open setup requiring internet. This is normal startup routing, subject to the existing synchronization recovery and expiration rules.

User clarification, 2026-09-27:

> The first version will be only for me. So it is hardcoded in the first version.

Version one is for the requesting user only. The Azure deployment address is hardcoded in the app. Endpoint configuration in later versions is not decided.

User clarification, 2026-09-27:

> yes, Admin.db if single user prototype before second version, where I am going to make MsSql db with multiusers

The “yes” confirms device registration in Admin.db before LocalDb download in the Open flow. Version one is a single-user prototype using SQLite Admin.db. Version two is planned to use Microsoft SQL Server for multi-user administration. This refines the earlier unspecified later-release timing in synchronization discovery. Existing SQLite MasterDb/LocalDb storage and one-owner-per-MasterDb rules are unchanged; shared ownership was not requested.

User clarification, 2026-09-27:

> User cannot continue without local db

LocalDb is required before proceeding to bookkeeping. If cloud creation succeeds but initial download fails, the user cannot enter normal app use without LocalDb. This answer confirms the access condition, not a particular retry button or automatic retry policy.

User clarification, 2026-09-27:

> Forget this. App checks only existence. Should it use "creating mode" or "editing mode"

The LocalDb check is an existence check for startup mode selection: absent means creating mode (Create/Open setup), present means editing mode (open existing LocalDb). Additional first-use file/schema/business-data validation was not requested. Existing synchronization recovery and download/activation rules remain separate from this mode-selection check.

Initial-data decision, 2026-09-27: user replied “agree” to the proposal “five required root groups + selected base currency with rate 1, with no accounts or transactions. Other currencies added later.” This confirms the version-one initial dataset; it is not artifact approval.

User clarification, 2026-09-27:

> Open mode doesn't require internet. This is the main sense of the sync. For create mode internet is mandatory

Open mode means ordinary use of the existing LocalDb and works offline. Creating mode requires internet. Distinguish this mode name from the Open button in initial setup, whose original purpose is downloading LocalDb from Azure. This clarification does not remove connectivity requirements from cloud download or synchronization.

## Rejected Ideas

None newly rejected. Existing rejection of independent offline-only LocalDb creation remains applicable.

## Deferred Topics

- Database-template selection: future versions; exact version and template contents unspecified. This is distinct from transaction templates in core bookkeeping.
- Multiple MasterDbs and LocalDb display names: version two under existing synchronization decisions.
- Multi-user administration with Microsoft SQL Server: version two (user confirmation, 2026-09-27). Migration details remain unspecified.
- Administration detail and formal BRD/TRD rework: later discussion. No implementation work in this discovery.

## Promotion Readiness

- Ready to seek discovery-to-business promotion: Not requested; continue brainstorming. Deployment scope is resolved.
- Questions blocking a safe labeled BRD draft: None currently identified; remaining questions stay explicitly unresolved.
- Other questions may remain explicitly unresolved in a draft, subject to resolution before approval.
- Promotion decision reference: None.
- Next recommended phase: Continue discovery, one question at a time.










