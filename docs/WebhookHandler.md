# WebhookHandler

The `WebhookHandler` class provides functionality to receive, validate, and process webhook events from external services, primarily for synchronizing tasks with Notion. It handles signature validation, payload size limits, and registration of supported webhook types.

## API

### `public WebhookHandler`

Initializes a new instance of the `WebhookHandler` class with default configuration.

- **Parameters**: None
- **Remarks**: Sets default values for `MaxPayloadSizeBytes` (10MB), `AllowedTypes` (empty list), and `Secret` (null).

---

### `public async Task<bool> HandleWebhookAsync`

Processes an incoming webhook request asynchronously, validates its signature and payload, and dispatches the event to registered handlers.

- **Parameters**:
  - `HttpContext context`: The HTTP context containing the webhook request.
  - `ILogger<WebhookHandler> logger`: Logger instance for recording processing events.
- **Return value**: `true` if the webhook was successfully processed; otherwise, `false`.
- **Exceptions**:
  - `ArgumentNullException`: Thrown if `context` is null.
  - `InvalidOperationException`: Thrown if no handlers are registered or the webhook type is unsupported.

---

### `public void RegisterHandler`

Registers a webhook type handler for a specific event type.

- **Parameters**:
  - `string type`: The webhook event type to register (e.g., "task.created").
  - `Func<WebhookPayload, Task<bool>> handler`: The asynchronous handler function to invoke when the event type is received.
- **Exceptions**:
  - `ArgumentNullException`: Thrown if `type` or `handler` is null.
  - `InvalidOperationException`: Thrown if the type is already registered.

---

### `public List<string> GetRegisteredWebhookTypes`

Returns a list of all registered webhook event types.

- **Return value**: A new list containing the names of all registered webhook types.
- **Remarks**: The returned list is a copy; modifications do not affect the internal state.

---

### `public bool ValidateWebhookSignature`

Validates the HMAC-SHA256 signature of a webhook payload against the configured secret.

- **Parameters**:
  - `string payload`: The raw payload body as a string.
  - `string signatureHeader`: The value of the `X-Notion-Signature` header.
- **Return value**: `true` if the signature is valid; otherwise, `false`.
- **Exceptions**:
  - `ArgumentNullException`: Thrown if `payload` or `signatureHeader` is null.

---
### `public string? Secret`

Gets or sets the secret used to validate webhook signatures.

- **Remarks**: Setting to `null` disables signature validation. Defaults to `null`.

---
### `public List<string> AllowedTypes`

Gets or sets the list of allowed webhook event types.

- **Remarks**: Only events with types in this list will be processed. Defaults to an empty list.

---
### `public int MaxPayloadSizeBytes`

Gets or sets the maximum allowed payload size in bytes.

- **Remarks**: Payloads exceeding this size will be rejected. Defaults to 10,485,760 (10MB).

---
### `public bool ValidateSignature`

Gets or sets a value indicating whether to validate the webhook signature.

- **Remarks**: If `false`, signature validation is skipped. Defaults to `true`.

## Usage

### Example 1: Basic Setup and Handling
