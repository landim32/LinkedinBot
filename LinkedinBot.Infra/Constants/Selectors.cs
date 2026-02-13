namespace LinkedinBot.Infra.Constants;

public static class Selectors
{
    // Job search results
    public const string JobCardContainer = ".job-card-container--clickable";
    public const string JobCardList = ".jobs-search-results-list";
    public const string ScaffoldLayoutList = ".scaffold-layout__list";

    // Job detail panel
    public const string JobTitle = ".job-details-jobs-unified-top-card__job-title";
    public const string CompanyName = ".job-details-jobs-unified-top-card__company-name";
    public const string JobDescription = "#job-details";
    public const string JobLocation = ".job-details-jobs-unified-top-card__primary-description-container";

    // Easy Apply
    public const string EasyApplyButton = ".jobs-apply-button";
    public const string ModalDialog = "[role='dialog']";
    public const string ModalContent = ".artdeco-modal__content";

    // Form elements inside modal
    public const string FormInput = "input[type='text'], input[type='tel'], input[type='email'], input[type='number']";
    public const string FormTextarea = "textarea";
    public const string FormSelect = "select";
    public const string FormFieldset = "fieldset";
    public const string FormLabel = "label";
    public const string FormError = ".artdeco-inline-feedback--error";

    // Job dismiss (X button on job detail panel)
    public const string JobDismissButton = "button[aria-label='Dispensar']";

    // Already applied indicator
    public const string AlreadyApplied = ".jobs-apply-button--applied";

    // Pagination
    public const string PaginationList = ".artdeco-pagination__pages";
    public const string PaginationButton = ".artdeco-pagination__indicator";

    // Safety reminder dialog
    public const string SafetyReminderContinueButtonText = "Continuar candidatura";

    // Buttons (pt-BR text)
    public const string NextButtonText = "Avançar";
    public const string ReviewButtonText = "Revisar";
    public const string SubmitButtonText = "Enviar candidatura";
    public const string DismissButtonText = "Dispensar";
    public const string DiscardButtonText = "Descartar";
    public const string AllFiltersButtonText = "Todos os filtros";
    public const string ShowResultsButtonText = "Mostrar";
    public const string EasyApplyButtonText = "Candidatura simplificada";
}
