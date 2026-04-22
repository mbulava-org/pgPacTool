# Embedded Skills and Analyzer Adoption Checklist

Use this checklist when changing significant repository behavior.

## 1. Skill Consultation
- [ ] Identify the relevant embedded skill(s)
- [ ] Review the linked research/reference docs
- [ ] Confirm whether the change is version-sensitive, platform-sensitive, or packaging-sensitive

## 2. Code Change Classification
- [ ] Compare/deploy behavior
- [ ] Catalog extraction behavior
- [ ] SDK behavior
- [ ] CLI UX/diagnostics behavior
- [ ] Native runtime/packaging behavior
- [ ] Test strategy behavior
- [ ] Generated project layout behavior
- [ ] Cloud-managed PostgreSQL behavior

## 3. Documentation Sync
- [ ] Update the relevant version-reference doc(s)
- [ ] Update the relevant embedded-skills research doc if the durable rule changed
- [ ] Update README or docs index if user-visible behavior changed
- [ ] Update `.github/copilot-instructions.md` if a durable guidance rule changed

## 4. Test Sync
- [ ] Add or update unit tests if logic changed
- [ ] Add or update integration tests if runtime PostgreSQL behavior changed
- [ ] Add package/runtime validation if packaging or native behavior changed
- [ ] Add Linux/container validation if platform-sensitive behavior changed

## 5. Analyzer Consideration
- [ ] Decide whether this change should be machine-detectable in the future
- [ ] If yes, map it to an analyzer family and candidate rule ID range
- [ ] If the rule already exists later, ensure tests/docs align with the enforced behavior

## 6. Cloud/Platform Consideration
- [ ] Check whether Azure Database for PostgreSQL behavior differs
- [ ] Check whether Amazon RDS for PostgreSQL behavior differs
- [ ] Check whether Amazon Aurora PostgreSQL-Compatible behavior differs
- [ ] Document provider-specific caveats if the behavior is not portable

## 7. Final Validation
- [ ] Build succeeds
- [ ] Relevant tests succeed
- [ ] Docs and skills still reflect actual behavior
- [ ] No secrets or unsafe assumptions were introduced

*Last updated: Current Session*
