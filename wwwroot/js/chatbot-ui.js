(function () {
    const form = document.getElementById("chatbotAskForm");
    if (!form) {
        return;
    }

    const promptBox = document.getElementById("chatPrompt");
    const sessionIdInput = document.getElementById("chatSessionId");
    const submitBtn = document.getElementById("chatSubmit");
    const retryBtn = document.getElementById("chatRetry");
    const errorBox = document.getElementById("chatbotError");
    const typing = document.getElementById("chatbotTyping");
    const history = document.getElementById("chatHistory");
    const answerContainer = document.getElementById("lastAnswerContainer");
    const answerText = document.getElementById("lastAnswerText");
    const answerMeta = document.getElementById("lastAnswerMeta");
    const sourceCard = document.getElementById("sourceCard");
    const sourceList = document.getElementById("sourceList");
    const language = document.getElementById("chatLanguage");
    const escalateBtn = document.getElementById("chatEscalate");
    const unresolvedBtn = document.getElementById("chatMarkUnresolved");
    const escalationCard = document.getElementById("escalationCard");

    const antiForgery = form.querySelector("input[name='__RequestVerificationToken']")?.value || "";
    let lastPrompt = "";
    let lastEscalationId = null;

    async function postForm(url, payload) {
        const body = new URLSearchParams(payload);
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
            },
            body: body.toString()
        });

        if (!response.ok) {
            let error = "Request failed.";
            try {
                const json = await response.json();
                error = json.error || error;
            } catch {
                // No-op: use default error.
            }
            throw new Error(error);
        }

        return response.json();
    }

    function setBusy(isBusy) {
        submitBtn.disabled = isBusy;
        retryBtn.disabled = isBusy || !lastPrompt;
        unresolvedBtn.disabled = isBusy || !sessionIdInput.value;
        escalateBtn.disabled = isBusy || !sessionIdInput.value;
        typing.classList.toggle("d-none", !isBusy);
    }

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }

    function clearError() {
        errorBox.textContent = "";
        errorBox.classList.add("d-none");
    }

    function renderSources(sources) {
        if (!Array.isArray(sources) || sources.length === 0) {
            sourceCard.classList.add("d-none");
            sourceList.innerHTML = "";
            return;
        }

        const rows = sources.map((s) => {
            const title = escapeHtml(s.sourceName || "Source");
            const type = escapeHtml(s.sourceType || "Reference");
            const path = escapeHtml(s.sourcePath || "");
            const excerpt = escapeHtml(s.excerpt || "");
            return `
<div class="border rounded p-2">
    <div class="small text-muted">${type}</div>
    <div class="fw-semibold">${title}</div>
    <div class="small">${excerpt}</div>
    <div class="small text-muted">${path}</div>
</div>`;
        });

        sourceList.innerHTML = rows.join("");
        sourceCard.classList.remove("d-none");
    }

    function renderAnswer(answer, confidence, escalation, category, languageCode) {
        const alertClass = escalation ? "alert alert-warning" : "alert alert-info";
        answerContainer.className = alertClass;
        answerContainer.classList.remove("d-none");
        answerText.textContent = answer || "";
        answerMeta.textContent = `Confidence: ${Number(confidence || 0).toFixed(2)} | Category: ${category || "General"} | Language: ${languageCode || "en"}`;
        escalationCard.classList.toggle("d-none", !escalation);
    }

    function renderHistory(items) {
        if (!Array.isArray(items) || items.length === 0) {
            history.innerHTML = '<p class="text-muted mb-0">No messages yet in this session.</p>';
            return;
        }

        const html = items.map((m) => {
            const isUser = (m.senderType || "").toLowerCase() === "user";
            const shellClass = isUser ? "bg-light" : "bg-primary-subtle";
            const safeSender = escapeHtml(m.senderType || "Assistant");
            const safeTime = escapeHtml(m.createdAtUtc || "");
            const safeContent = escapeHtml(m.content || "").replace(/\n/g, "<br>");
            const feedback = isUser
                ? ""
                : `<div class="mt-2 d-flex gap-2">
<button type="button" class="btn btn-sm btn-outline-success chat-feedback" data-feedback="Helpful" data-message-id="${m.id}">Helpful</button>
<button type="button" class="btn btn-sm btn-outline-danger chat-feedback" data-feedback="NotHelpful" data-message-id="${m.id}">Not Helpful</button>
</div>`;

            return `
<div class="mb-3 p-3 rounded ${shellClass}">
    <div class="small text-muted mb-1">${safeSender} | ${safeTime} UTC</div>
    <div>${safeContent}</div>
    ${feedback}
</div>`;
        }).join("");

        history.innerHTML = html;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    async function ask(prompt) {
        clearError();
        setBusy(true);

        try {
            const data = await postForm(form.dataset.askUrl, {
                __RequestVerificationToken: antiForgery,
                sessionId: sessionIdInput.value || "",
                languageCode: language.value || "en",
                prompt: prompt
            });

            sessionIdInput.value = data.sessionId || "";
            lastEscalationId = data.escalationId || null;
            lastPrompt = prompt;
            retryBtn.disabled = false;
            unresolvedBtn.disabled = false;
            escalateBtn.disabled = false;
            promptBox.value = "";

            renderAnswer(data.answer, data.confidenceScore, data.escalationSuggested, data.detectedCategory, data.detectedLanguage);
            renderSources(data.sources);
            renderHistory(data.history);
        } catch (error) {
            showError(error.message || "Unable to send message.");
        } finally {
            setBusy(false);
        }
    }

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        const prompt = (promptBox.value || "").trim();
        if (!prompt) {
            showError("Please enter a message for the assistant.");
            return;
        }

        ask(prompt);
    });

    retryBtn.addEventListener("click", function () {
        if (!lastPrompt) {
            return;
        }
        ask(lastPrompt);
    });

    history.addEventListener("click", async function (event) {
        const target = event.target;
        if (!(target instanceof HTMLElement) || !target.classList.contains("chat-feedback")) {
            return;
        }

        const feedbackType = target.dataset.feedback;
        const messageId = target.dataset.messageId;
        if (!feedbackType || !messageId || !sessionIdInput.value) {
            return;
        }

        try {
            await postForm(form.dataset.feedbackUrl, {
                __RequestVerificationToken: antiForgery,
                sessionId: sessionIdInput.value,
                messageId: messageId,
                feedbackType: feedbackType
            });

            target.textContent = "Saved";
            target.setAttribute("disabled", "disabled");
        } catch {
            showError("Unable to save feedback right now.");
        }
    });

    unresolvedBtn.addEventListener("click", async function () {
        if (!sessionIdInput.value) {
            return;
        }

        try {
            await postForm(form.dataset.unresolvedUrl, {
                __RequestVerificationToken: antiForgery,
                sessionId: sessionIdInput.value,
                reason: "User marked conversation unresolved"
            });

            unresolvedBtn.textContent = "Marked";
            unresolvedBtn.setAttribute("disabled", "disabled");
            escalationCard.classList.remove("d-none");
        } catch {
            showError("Unable to mark session unresolved right now.");
        }
    });

    escalateBtn.addEventListener("click", async function () {
        if (!sessionIdInput.value) {
            return;
        }

        try {
            const data = await postForm(form.dataset.escalateUrl, {
                __RequestVerificationToken: antiForgery,
                sessionId: sessionIdInput.value,
                reason: "User requested support handoff",
                escalationType: "Support"
            });

            lastEscalationId = data.escalationId || lastEscalationId;
            escalateBtn.textContent = "Escalated";
            escalateBtn.setAttribute("disabled", "disabled");
            answerMeta.textContent = `${answerMeta.textContent} | Escalation #${lastEscalationId}`;
        } catch {
            showError("Unable to create escalation right now.");
        }
    });
})();
