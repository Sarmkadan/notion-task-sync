# Security Policy

## Supported Versions

| Version | Status |
|---------|--------|
| v2.0.x | ✅ Supported |
| v1.x    | ⚠️ Security fixes only |


## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it responsibly. **Do not report security issues through public GitHub issues.**


### How to Report

Send an email to: **rutova2@gmail.com**


Please include the following information:
- Type of issue (e.g., buffer overflow, SQL injection, authentication bypass)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit the issue

### Response Process

1. You will receive an acknowledgment within **48 hours**
2. The vulnerability will be investigated by the maintainers
3. A patch will be released as soon as possible
4. You will be notified when the vulnerability is fixed

## Security Best Practices

### For Users
- Always use the latest supported version (v2.0.x)
- Store API keys securely using environment variables or secure configuration
- Regularly rotate Notion API keys and other credentials
- Monitor logs for suspicious activity
- Use HTTPS endpoints only

### For Contributors
- Follow secure coding practices
- Validate all inputs to prevent injection attacks
- Never log sensitive information (API keys, tokens, passwords)
- Use parameterized queries for database operations
- Implement proper error handling to avoid information leakage

## Disclosure Policy

This is a **coordinated disclosure** process:
- Do not publicly disclose vulnerabilities before they are fixed
- Give the maintainers time to respond and release a fix
- Credit will be given to reporters in release notes (unless requested otherwise)

## Security Updates

Security updates will be released as patch versions for the affected major/minor versions. Check the [CHANGELOG.md](CHANGELOG.md) for security-related changes.

## Additional Resources

- [Notion API Security](https://developers.notion.com/docs/security)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)
