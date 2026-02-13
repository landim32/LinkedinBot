using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Constants;
using LinkedinBot.Infra.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Services;

public class LinkedInApplyService : ILinkedInApplyService
{
    private readonly IChatGptService _chatGptService;
    private readonly ILogger<LinkedInApplyService> _logger;
    private readonly int _maxFormSteps;

    public LinkedInApplyService(
        IChatGptService chatGptService,
        IOptions<JobSearchSettings> settings,
        ILogger<LinkedInApplyService> logger)
    {
        _chatGptService = chatGptService;
        _maxFormSteps = settings.Value.MaxFormSteps;
        _logger = logger;
    }

    public async Task<bool> ApplyToJobAsync(IPage page, JobListing job, Func<int, bool>? onUnrecognizedAction = null)
    {
        try
        {
            if (!await ClickEasyApplyButtonAsync(page))
            {
                _logger.LogWarning("Could not find Easy Apply button for: {Title}", job.Title);
                return false;
            }

            // Handle safety reminder dialog if it appears
            await DismissSafetyReminderAsync(page);

            var modal = page.Locator(Selectors.ModalDialog);
            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });

            _logger.LogInformation("Easy Apply modal opened for: {Title}", job.Title);

            for (var step = 0; step < _maxFormSteps; step++)
            {
                _logger.LogInformation("=== Form step {Step}/{Max} ===", step + 1, _maxFormSteps);

                await Task.Delay(Random.Shared.Next(1000, 2000));

                await FillCurrentStepAsync(page, modal);

                // Check for validation errors before attempting to advance
                if (await HasValidationErrorsAsync(modal))
                {
                    _logger.LogWarning("Validation errors detected before advancing at step {Step}", step + 1);
                }

                var action = await GetNextActionAsync(modal);
                _logger.LogInformation("Step {Step} action: {Action}", step + 1, action);

                switch (action)
                {
                    case FormAction.Submit:
                        _logger.LogInformation("Submitting application for: {Title}", job.Title);
                        await ClickButtonByTextAsync(modal, Selectors.SubmitButtonText);
                        await Task.Delay(2000);
                        await DismissSuccessDialogAsync(page);
                        return true;

                    case FormAction.Review:
                        _logger.LogInformation("Clicking Review...");
                        await ClickButtonByTextAsync(modal, Selectors.ReviewButtonText);
                        await Task.Delay(1500);
                        break;

                    case FormAction.Next:
                        _logger.LogInformation("Clicking Next (step {Step})...", step + 1);
                        await ClickButtonByTextAsync(modal, Selectors.NextButtonText);
                        await Task.Delay(1500);

                        if (await HasValidationErrorsAsync(modal))
                        {
                            _logger.LogWarning("Validation errors found at step {Step}. Attempting to fix...", step + 1);
                            await Task.Delay(1000);
                            await FillCurrentStepAsync(page, modal);
                            await ClickButtonByTextAsync(modal, Selectors.NextButtonText);
                            await Task.Delay(1500);

                            if (await HasValidationErrorsAsync(modal))
                            {
                                _logger.LogError("Could not resolve validation errors. Aborting application.");
                                await DismissModalAsync(page, modal);
                                return false;
                            }
                        }
                        break;

                    case FormAction.Unknown:
                        _logger.LogWarning("No recognized action button found at step {Step}.", step + 1);

                        if (onUnrecognizedAction is not null)
                        {
                            var shouldContinue = onUnrecognizedAction(step + 1);
                            if (shouldContinue)
                            {
                                // User resolved it manually — re-check this step
                                step--;
                                continue;
                            }

                            // User chose to stop — signal via OperationCanceledException
                            _logger.LogInformation("User requested bot shutdown.");
                            throw new OperationCanceledException("User requested stop from unrecognized action prompt.");
                        }

                        // No callback — dismiss and fail
                        await DismissModalAsync(page, modal);
                        return false;
                }
            }

            _logger.LogWarning("Reached max form steps ({Max}).", _maxFormSteps);

            if (onUnrecognizedAction is not null)
            {
                var shouldContinue = onUnrecognizedAction(_maxFormSteps);
                if (shouldContinue)
                {
                    // User resolved it manually — restart form loop from current state
                    for (var extraStep = 0; extraStep < _maxFormSteps; extraStep++)
                    {
                        _logger.LogInformation("=== Extra form step {Step}/{Max} ===", extraStep + 1, _maxFormSteps);

                        await Task.Delay(Random.Shared.Next(1000, 2000));
                        await FillCurrentStepAsync(page, modal);

                        if (await HasValidationErrorsAsync(modal))
                            _logger.LogWarning("Validation errors detected at extra step {Step}", extraStep + 1);

                        var action = await GetNextActionAsync(modal);
                        _logger.LogInformation("Extra step {Step} action: {Action}", extraStep + 1, action);

                        switch (action)
                        {
                            case FormAction.Submit:
                                _logger.LogInformation("Submitting application for: {Title}", job.Title);
                                await ClickButtonByTextAsync(modal, Selectors.SubmitButtonText);
                                await Task.Delay(2000);
                                await DismissSuccessDialogAsync(page);
                                return true;

                            case FormAction.Review:
                                _logger.LogInformation("Clicking Review...");
                                await ClickButtonByTextAsync(modal, Selectors.ReviewButtonText);
                                await Task.Delay(1500);
                                break;

                            case FormAction.Next:
                                _logger.LogInformation("Clicking Next (extra step {Step})...", extraStep + 1);
                                await ClickButtonByTextAsync(modal, Selectors.NextButtonText);
                                await Task.Delay(1500);
                                break;

                            case FormAction.Unknown:
                                _logger.LogWarning("No recognized action at extra step {Step}.", extraStep + 1);
                                var continueAgain = onUnrecognizedAction(extraStep + 1);
                                if (continueAgain)
                                {
                                    extraStep--;
                                    continue;
                                }
                                _logger.LogInformation("User requested bot shutdown.");
                                throw new OperationCanceledException("User requested stop.");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("User requested bot shutdown.");
                    throw new OperationCanceledException("User requested stop from max steps prompt.");
                }
            }

            _logger.LogWarning("Aborting application after max steps.");
            await DismissModalAsync(page, modal);
            return false;
        }
        catch (OperationCanceledException)
        {
            throw; // Let it propagate — user requested stop or Ctrl+C
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Easy Apply for: {Title}", job.Title);

            try
            {
                var modal = page.Locator(Selectors.ModalDialog);
                if (await modal.IsVisibleAsync())
                    await DismissModalAsync(page, modal);
            }
            catch
            {
                // Ignore cleanup errors
            }

            return false;
        }
    }

    private async Task<bool> ClickEasyApplyButtonAsync(IPage page)
    {
        try
        {
            var button = page.Locator(Selectors.EasyApplyButton);
            if (await button.CountAsync() > 0 && await button.First.IsVisibleAsync())
            {
                await button.First.ClickAsync();
                return true;
            }

            var buttonByText = page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = Selectors.EasyApplyButtonText });
            if (await buttonByText.CountAsync() > 0)
            {
                await buttonByText.First.ClickAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clicking Easy Apply button");
            return false;
        }
    }

    private async Task FillCurrentStepAsync(IPage page, ILocator modal)
    {
        await FillTextInputsAsync(modal);
        await FillTextareasAsync(modal);
        await FillSelectsAsync(modal);
        await FillRadioGroupsAsync(modal);
    }

    private async Task FillTextInputsAsync(ILocator modal)
    {
        var inputs = modal.Locator(Selectors.FormInput);
        var count = await inputs.CountAsync();

        for (var i = 0; i < count; i++)
        {
            try
            {
                var input = inputs.Nth(i);

                if (!await input.IsVisibleAsync() || !await input.IsEditableAsync())
                    continue;

                var currentValue = await input.InputValueAsync();
                if (!string.IsNullOrWhiteSpace(currentValue))
                    continue;

                var questionText = await GetLabelForInputAsync(modal, input);
                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                var inputType = await input.GetAttributeAsync("type") ?? "text";
                var answer = await _chatGptService.AnswerFormQuestionAsync(questionText);

                // Sanitize: numeric/phone fields must contain only digits
                var isNumericField = inputType is "number" or "tel";
                var isPhoneByLabel = questionText.Contains("celular", StringComparison.OrdinalIgnoreCase)
                                     || questionText.Contains("telefone", StringComparison.OrdinalIgnoreCase)
                                     || questionText.Contains("phone", StringComparison.OrdinalIgnoreCase)
                                     || questionText.Contains("whatsapp", StringComparison.OrdinalIgnoreCase);

                if (isNumericField || isPhoneByLabel)
                {
                    var digitsOnly = new string(answer.Where(char.IsDigit).ToArray());
                    if (string.IsNullOrEmpty(digitsOnly))
                        digitsOnly = "0";
                    _logger.LogInformation("Sanitized numeric answer: '{Original}' → '{Sanitized}'", answer, digitsOnly);
                    answer = digitsOnly;
                }

                await input.FillAsync(answer);

                _logger.LogInformation("Filled input '{Question}' with '{Answer}' (type={Type})", questionText, answer, inputType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error filling text input {Index}", i);
            }
        }
    }

    private async Task FillTextareasAsync(ILocator modal)
    {
        var textareas = modal.Locator(Selectors.FormTextarea);
        var count = await textareas.CountAsync();

        for (var i = 0; i < count; i++)
        {
            try
            {
                var textarea = textareas.Nth(i);

                if (!await textarea.IsVisibleAsync() || !await textarea.IsEditableAsync())
                    continue;

                var currentValue = await textarea.InputValueAsync();
                if (!string.IsNullOrWhiteSpace(currentValue))
                    continue;

                var questionText = await GetLabelForInputAsync(modal, textarea);
                if (string.IsNullOrWhiteSpace(questionText))
                    questionText = "Additional information or cover letter";

                var answer = await _chatGptService.AnswerFormQuestionAsync(questionText);
                await textarea.FillAsync(answer);

                _logger.LogInformation("Filled textarea '{Question}'", questionText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error filling textarea {Index}", i);
            }
        }
    }

    private async Task FillSelectsAsync(ILocator modal)
    {
        var selects = modal.Locator(Selectors.FormSelect);
        var count = await selects.CountAsync();
        _logger.LogInformation("Found {Count} select(s) to process", count);

        for (var i = 0; i < count; i++)
        {
            try
            {
                var select = selects.Nth(i);

                if (!await select.IsVisibleAsync())
                {
                    _logger.LogInformation("Select {Index} is not visible, skipping", i);
                    continue;
                }

                var questionText = await GetLabelForInputAsync(modal, select);
                _logger.LogInformation("Select {Index} label: '{Label}'", i, questionText);

                // Check if already has a valid selection (not the placeholder)
                var currentValue = await select.InputValueAsync();
                _logger.LogInformation("Select {Index} current value attribute: '{Value}'", i, currentValue?.Trim());
                if (!string.IsNullOrWhiteSpace(currentValue) && !IsPlaceholderOption(currentValue))
                {
                    _logger.LogInformation("Select already has valid value: '{Value}', skipping", currentValue.Trim());
                    continue;
                }

                // Collect option value/text pairs for reliable selection
                var optionElements = select.Locator("option");
                var optionCount = await optionElements.CountAsync();
                var optionPairs = new List<(string Value, string Text)>();

                for (var j = 0; j < optionCount; j++)
                {
                    var opt = optionElements.Nth(j);
                    var val = (await opt.GetAttributeAsync("value") ?? "").Trim();
                    var txt = (await opt.InnerTextAsync()).Trim();
                    optionPairs.Add((val, txt));
                }

                _logger.LogInformation("Select options found: [{Options}]",
                    string.Join(", ", optionPairs.Select(p => $"{p.Text} (value={p.Value})")));

                var validPairs = optionPairs
                    .Where(p => !string.IsNullOrWhiteSpace(p.Text) && !IsPlaceholderOption(p.Text))
                    .ToList();

                if (validPairs.Count == 0)
                {
                    _logger.LogWarning("No valid options found for select {Index}", i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(questionText))
                {
                    _logger.LogWarning("No label found for select {Index}, skipping", i);
                    continue;
                }

                var validTexts = validPairs.Select(p => p.Text).ToList();
                var answer = await _chatGptService.AnswerFormQuestionAsync(questionText, validTexts);
                var bestMatchText = FindBestMatch(answer, validTexts);
                var bestMatchValue = validPairs.First(p => p.Text == bestMatchText).Value;

                _logger.LogInformation("Q: {Question} → A: {Answer} (matched: '{Match}', value: '{Value}')",
                    questionText, answer, bestMatchText, bestMatchValue);

                await select.SelectOptionAsync(new SelectOptionValue { Value = bestMatchValue });
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error filling select {Index}", i);
            }
        }
    }

    private static bool IsPlaceholderOption(string text)
    {
        var normalized = text.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized)
               || normalized.Contains("selecionar")  // pt-BR
               || normalized.Contains("selecciona")   // es
               || normalized.Contains("select")        // en
               || normalized.Contains("escolha")       // pt-BR
               || normalized.Contains("choose")        // en
               || normalized.Contains("elige")         // es
               || normalized.Contains("opción")        // es
               || normalized.Contains("opção")         // pt-BR
               || normalized == "--";
    }

    private async Task FillRadioGroupsAsync(ILocator modal)
    {
        var fieldsets = modal.Locator(Selectors.FormFieldset);
        var count = await fieldsets.CountAsync();

        for (var i = 0; i < count; i++)
        {
            try
            {
                var fieldset = fieldsets.Nth(i);

                if (!await fieldset.IsVisibleAsync())
                    continue;

                var radios = fieldset.Locator("input[type='radio']");
                if (await radios.CountAsync() == 0)
                    continue;

                var checkedRadio = fieldset.Locator("input[type='radio']:checked");
                if (await checkedRadio.CountAsync() > 0)
                    continue;

                var legend = fieldset.Locator("legend, span.fb-dash-form-element__label");
                var questionText = await legend.CountAsync() > 0
                    ? await legend.First.InnerTextAsync()
                    : "";

                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                var labels = fieldset.Locator("label");
                var labelTexts = (await labels.AllInnerTextsAsync()).ToList();

                if (labelTexts.Count == 0)
                    continue;

                var answer = await _chatGptService.AnswerFormQuestionAsync(questionText, labelTexts);
                var bestMatch = FindBestMatch(answer, labelTexts);

                var targetLabel = fieldset.Locator($"label:has-text('{EscapeForSelector(bestMatch)}')");
                if (await targetLabel.CountAsync() > 0)
                {
                    await targetLabel.First.ClickAsync();
                    _logger.LogInformation("Selected radio '{Answer}' for '{Question}'", bestMatch, questionText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error filling radio group {Index}", i);
            }
        }
    }


    private async Task<FormAction> GetNextActionAsync(ILocator modal)
    {
        if (await FindButtonBySpanTextAsync(modal, Selectors.SubmitButtonText) is not null)
            return FormAction.Submit;

        if (await FindButtonBySpanTextAsync(modal, Selectors.ReviewButtonText) is not null)
            return FormAction.Review;

        if (await FindButtonBySpanTextAsync(modal, Selectors.NextButtonText) is not null)
            return FormAction.Next;

        return FormAction.Unknown;
    }

    private async Task ClickButtonByTextAsync(ILocator modal, string buttonText)
    {
        var button = await FindButtonBySpanTextAsync(modal, buttonText);
        if (button is not null)
        {
            await button.ClickAsync();
            return;
        }

        // Fallback: GetByRole
        var roleButton = modal.GetByRole(AriaRole.Button,
            new LocatorGetByRoleOptions { Name = buttonText });
        await roleButton.First.ClickAsync();
    }

    private static async Task<ILocator?> FindButtonBySpanTextAsync(ILocator container, string text)
    {
        var button = container.Locator($"button:has(span.artdeco-button__text:text-is('{text}'))");
        if (await button.CountAsync() > 0 && await button.First.IsVisibleAsync())
            return button.First;

        return null;
    }

    private async Task<bool> HasValidationErrorsAsync(ILocator modal)
    {
        await Task.Delay(500);
        var errors = modal.Locator(Selectors.FormError);
        var count = await errors.CountAsync();
        if (count > 0)
        {
            var errorTexts = await errors.AllInnerTextsAsync();
            _logger.LogWarning("Validation errors: {Errors}", string.Join("; ", errorTexts));
        }
        return count > 0;
    }

    private async Task DismissSafetyReminderAsync(IPage page)
    {
        try
        {
            await Task.Delay(1500);

            // Try finding the "Continuar candidatura" button by span text (LinkedIn pattern)
            var continueButton = page.Locator(
                $"button:has(span:text-is('{Selectors.SafetyReminderContinueButtonText}'))");

            if (await continueButton.CountAsync() > 0 && await continueButton.First.IsVisibleAsync())
            {
                await continueButton.First.ClickAsync();
                _logger.LogInformation("Safety reminder dismissed — clicked '{Button}'",
                    Selectors.SafetyReminderContinueButtonText);
                await Task.Delay(1500);
                return;
            }

            // Fallback: GetByRole
            var roleButton = page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = Selectors.SafetyReminderContinueButtonText });

            if (await roleButton.CountAsync() > 0 && await roleButton.First.IsVisibleAsync())
            {
                await roleButton.First.ClickAsync();
                _logger.LogInformation("Safety reminder dismissed via role — clicked '{Button}'",
                    Selectors.SafetyReminderContinueButtonText);
                await Task.Delay(1500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No safety reminder dialog detected (this is normal)");
        }
    }

    private async Task DismissSuccessDialogAsync(IPage page)
    {
        try
        {
            await Task.Delay(2000);

            var dismissButton = page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = Selectors.DismissButtonText });

            if (await dismissButton.CountAsync() > 0 && await dismissButton.First.IsVisibleAsync())
            {
                await dismissButton.First.ClickAsync();
                _logger.LogInformation("Dismissed success dialog");
            }

            var closeButton = page.Locator("[role='dialog'] button[aria-label='Dismiss'], [role='dialog'] button[aria-label='Dispensar']");
            if (await closeButton.CountAsync() > 0 && await closeButton.First.IsVisibleAsync())
            {
                await closeButton.First.ClickAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Could not dismiss success dialog (may have auto-closed)");
        }
    }

    private async Task DismissModalAsync(IPage page, ILocator modal)
    {
        try
        {
            var dismissButton = modal.GetByRole(AriaRole.Button,
                new LocatorGetByRoleOptions { Name = Selectors.DismissButtonText });

            if (await dismissButton.CountAsync() > 0)
            {
                await dismissButton.First.ClickAsync();
            }
            else
            {
                var closeButton = modal.Locator("button[aria-label='Dismiss'], button[aria-label='Dispensar']");
                if (await closeButton.CountAsync() > 0)
                    await closeButton.First.ClickAsync();
            }

            await Task.Delay(1000);

            var discardButton = page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = Selectors.DiscardButtonText });
            if (await discardButton.CountAsync() > 0 && await discardButton.First.IsVisibleAsync())
            {
                await discardButton.First.ClickAsync();
                _logger.LogInformation("Discarded in-progress application");
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Error dismissing modal");
        }
    }

    private static async Task<string> GetLabelForInputAsync(ILocator modal, ILocator input)
    {
        try
        {
            var inputId = await input.GetAttributeAsync("id");
            if (!string.IsNullOrWhiteSpace(inputId))
            {
                var label = modal.Locator($"label[for='{inputId}']");
                if (await label.CountAsync() > 0)
                    return (await label.First.InnerTextAsync()).Trim();
            }

            var ariaLabel = await input.GetAttributeAsync("aria-label");
            if (!string.IsNullOrWhiteSpace(ariaLabel))
                return ariaLabel.Trim();

            var placeholder = await input.GetAttributeAsync("placeholder");
            if (!string.IsNullOrWhiteSpace(placeholder))
                return placeholder.Trim();

            var parentLabel = input.Locator("xpath=ancestor::div[contains(@class, 'form')]//label");
            if (await parentLabel.CountAsync() > 0)
                return (await parentLabel.First.InnerTextAsync()).Trim();
        }
        catch
        {
            // Ignore label extraction errors
        }

        return string.Empty;
    }

    private static string FindBestMatch(string answer, List<string> options)
    {
        var exact = options.FirstOrDefault(o =>
            o.Equals(answer, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        var contains = options.FirstOrDefault(o =>
            o.Contains(answer, StringComparison.OrdinalIgnoreCase)
            || answer.Contains(o, StringComparison.OrdinalIgnoreCase));
        if (contains is not null)
            return contains;

        return options.First();
    }

    private static string EscapeForSelector(string text)
    {
        return text.Replace("'", "\\'");
    }

    private enum FormAction
    {
        Next,
        Review,
        Submit,
        Unknown
    }
}
