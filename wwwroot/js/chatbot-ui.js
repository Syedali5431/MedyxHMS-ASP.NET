(function () {
    const form = document.getElementById("chatbotAskForm");
    if (!form) {
        return;
    }

    const askUrl = form.dataset.askUrl;
    const feedbackUrl = form.dataset.feedbackUrl;
    const escalateUrl = form.dataset.escalateUrl;
    const unresolvedUrl = form.dataset.unresolvedUrl;

    const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryToken = tokenInput ? tokenInput.value : "";

    const promptInput = document.getElementById("chatPrompt");
    const sessionInput = document.getElementById("chatSessionId");
    const languageSelect = document.getElementById("chatLanguage");
    const submitButton = document.getElementById("chatSubmit");
    const retryButton = document.getElementById("chatRetry");
    const unresolvedButton = document.getElementById("chatMarkUnresolved");
    const escalateButton = document.getElementById("chatEscalate");

    const errorBox = document.getElementById("chatbotError");
    const typingIndicator = document.getElementById("chatbotTyping");
    const answerContainer = document.getElementById("lastAnswerContainer");
    const answerText = document.getElementById("lastAnswerText");
    const answerMeta = document.getElementById("lastAnswerMeta");
    const sourceCard = document.getElementById("sourceCard");
    const sourceList = document.getElementById("sourceList");
    const chatHistory = document.getElementById("chatHistory");
    const escalationCard = document.getElementById("escalationCard");

    let lastRequest = null;

    function setBusy(isBusy) {
        submitButton.disabled = isBusy;
        typingIndicator.classList.toggle("d-none", !isBusy);
    }

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }

    function clearError() {
        errorBox.textContent = "";
        errorBox.classList.add("d-none");
    }

    function encodeForm(payload) {
        return Object.entries(payload)
            .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value ?? "")}`)
            .join("&");
    }

    function escapeHtml(value) {
        const text = String(value ?? "");
        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function buildMessageHtml(message) {
        const sender = message.senderType || "Assistant";
        const timestamp = message.createdAtUtc || "";
        const isUser = sender.toLowerCase() === "user";
        const content = message.content || "";

        const wrapper = document.createElement("div");
        wrapper.className = `mb-3 p-3 rounded ${isUser ? "bg-light" : "bg-primary-subtle"}`;
        wrapper.innerHTML = `
            <div class="small text-muted mb-1">${escapeHtml(sender)} | ${escapeHtml(timestamp)} UTC</div>
            <div>${escapeHtml(content)}</div>
        `;

        if (!isUser && message.id) {
            const actions = document.createElement("div");
            actions.className = "mt-2 d-flex gap-2";
            actions.innerHTML = `
                <button type="button" class="btn btn-sm btn-outline-success chat-feedback" data-feedback="Helpful" data-message-id="${message.id}">Helpful</button>
                <button type="button" class="btn btn-sm btn-outline-danger chat-feedback" data-feedback="NotHelpful" data-message-id="${message.id}">Not Helpful</button>
            `;
            wrapper.appendChild(actions);
        }

        return wrapper;
    }

    function renderHistory(history) {
        if (!chatHistory) {
            return;
        }

        chatHistory.innerHTML = "";
        if (!history || history.length === 0) {
            chatHistory.innerHTML = '<p class="text-muted mb-0">No messages yet in this session.</p>';
            return;
        }

        history.forEach((message) => {
            chatHistory.appendChild(buildMessageHtml(message));
        });
    }

    function renderSources(sources) {
        if (!sourceCard || !sourceList) {
            return;
        }

        sourceList.innerHTML = "";
        if (!sources || sources.length === 0) {
            sourceCard.classList.add("d-none");
            return;
        }

        sources.forEach((source) => {
            const item = document.createElement("div");
            item.className = "border rounded p-2 bg-light-subtle";
            const excerpt = source.excerpt ? `<div class="small text-muted mt-1">${escapeHtml(source.excerpt)}</div>` : "";
            const path = source.sourcePath ? `<div class="small text-secondary">${escapeHtml(source.sourcePath)}</div>` : "";
            item.innerHTML = `
                <div><strong>${escapeHtml(source.sourceName || "Knowledge Source")}</strong> <span class="text-muted">(${escapeHtml(source.sourceType || "Internal")})</span></div>
                ${path}
                ${excerpt}
            `;
            sourceList.appendChild(item);
        });

        sourceCard.classList.remove("d-none");
    }

    function renderAnswer(data) {
        answerText.textContent = data.answer || "";
        answerMeta.textContent = `Confidence: ${(Number(data.confidenceScore || 0)).toFixed(2)} | Category: ${data.detectedCategory || "General"}`;
        answerContainer.classList.remove("d-none");

        if (data.escalationSuggested) {
            answerContainer.classList.remove("alert-info", "alert-light");
            answerContainer.classList.add("alert-warning");
            escalationCard.classList.remove("d-none");
        } else {
            answerContainer.classList.remove("alert-warning", "alert-light");
            answerContainer.classList.add("alert-info");
            escalationCard.classList.add("d-none");
        }
    }

    async function postForm(url, payload) {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
            },
            body: encodeForm({ ...payload, __RequestVerificationToken: antiForgeryToken })
        });

        const text = await response.text();
        const json = text ? JSON.parse(text) : {};

        if (!response.ok) {
            throw new Error(json.error || "Request failed.");
        }

        return json;
    }

    async function ask(payload) {
        setBusy(true);
        clearError();

        try {
            const data = await postForm(askUrl, payload);
            sessionInput.value = data.sessionId || "";
            renderAnswer(data);
            renderSources(data.sources || []);
            renderHistory(data.history || []);

            retryButton.disabled = false;
            unresolvedButton.disabled = !(data.sessionId && data.escalationSuggested);
            lastRequest = payload;
            promptInput.value = "";
        } catch (error) {
            showError(error.message || "Unable to get a response from the assistant.");
        } finally {
            setBusy(false);
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        const prompt = (promptInput.value || "").trim();
        if (!prompt) {
            showError("Please enter a message for the assistant.");
            return;
        }

        await ask({
            prompt,
            sessionId: sessionInput.value || "",
            languageCode: languageSelect ? languageSelect.value : "en"
        });
    });

    retryButton.addEventListener("click", async function () {
        if (!lastRequest) {
            return;
        }

        await ask(lastRequest);
    });

    unresolvedButton.addEventListener("click", async function () {
        const sessionId = sessionInput.value || "";
        if (!sessionId) {
            showError("No active session found to mark unresolved.");
            return;
        }

        try {
            await postForm(unresolvedUrl, {
                sessionId,
                reason: "UserMarkedUnresolved"
            });
            unresolvedButton.disabled = true;
        } catch (error) {
            showError(error.message || "Unable to mark session unresolved.");
        }
    });

    if (escalateButton) {
        escalateButton.addEventListener("click", async function () {
            const sessionId = sessionInput.value || "";
            if (!sessionId) {
                showError("No active session found to escalate.");
                return;
            }

            try {
                const escalation = await postForm(escalateUrl, {
                    sessionId,
                    reason: "User requested escalation from chatbot UI",
                    escalationType: "Support"
                });

                showError(`Escalation created with ID ${escalation.escalationId}. Support will follow up shortly.`);
            } catch (error) {
                showError(error.message || "Unable to escalate the session.");
            }
        });
    }

    document.addEventListener("click", async function (event) {
        const target = event.target;
        if (!(target instanceof HTMLElement) || !target.classList.contains("chat-feedback")) {
            return;
        }

        const messageId = target.dataset.messageId;
        const feedbackType = target.dataset.feedback;
        const sessionId = sessionInput.value || "";

        if (!sessionId || !messageId || !feedbackType) {
            return;
        }

        try {
            await postForm(feedbackUrl, {
                sessionId,
                messageId,
                feedbackType,
                comment: ""
            });

            target.disabled = true;
        } catch (error) {
            showError(error.message || "Unable to submit feedback.");
        }
    });
})();
