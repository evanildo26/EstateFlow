using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

// Firm Administrator screen: staff accounts, estate assignment, the POPIA
// audit log, letter wording and the record-retention review.
public class AdminViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;

    public AdminViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());

        GeneratePasswordCommand = new RelayCommand(_ => NewStaffPassword = TemporaryPassword());
        CreateStaffCommand = new AsyncRelayCommand(async _ => await CreateStaffAsync());
        ResetPasswordCommand = new AsyncRelayCommand(async p => await StaffActionAsync(p, "reset-password"));
        UnlockCommand = new AsyncRelayCommand(async p => await StaffActionAsync(p, "unlock"));
        DeactivateCommand = new AsyncRelayCommand(async p => await StaffActionAsync(p, "deactivate"));
        ReactivateCommand = new AsyncRelayCommand(async p => await StaffActionAsync(p, "reactivate"));
        ResetTwoFactorCommand = new AsyncRelayCommand(async p => await StaffActionAsync(p, "reset-2fa"));

        AssignSelectedEstateCommand = new AsyncRelayCommand(async _ => await AssignSelectedEstateAsync());
        MoveAllEstatesCommand = new AsyncRelayCommand(async _ => await MoveAllEstatesAsync());

        LoadAuditCommand = new AsyncRelayCommand(async _ => await LoadAuditAsync());

        SaveTemplateCommand = new AsyncRelayCommand(async _ => await SaveTemplateAsync());
        ResetTemplateCommand = new AsyncRelayCommand(async _ => await ResetTemplateAsync());

        LoadRetentionCommand = new AsyncRelayCommand(async _ => await LoadRetentionAsync());
        PurgeEstateCommand = new AsyncRelayCommand(async p => await PurgeAsync(p));
    }

    // ─────────────────────────────────────────────
    // Shared
    // ─────────────────────────────────────────────

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    // A temporary password to hand to a staff member; shown once.
    private string _issuedPassword = string.Empty;
    public string IssuedPassword
    {
        get => _issuedPassword;
        set => SetProperty(ref _issuedPassword, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        await Run(async () =>
        {
            await LoadStaffAsync();
            await LoadEstatesAsync();
            await LoadTemplatesAsync();
        }, null);

        await LoadAuditAsync();
        await LoadRetentionAsync();
    }

    private async Task Run(Func<Task> work, string? success)
    {
        try
        {
            IsBusy = true;
            await work();
            if (success is not null)
                Message = success;
        }
        catch (Exception ex)
        {
            Message = ErrorReporter.Describe(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool Confirm(string text, string title, MessageBoxImage image = MessageBoxImage.Question) =>
        MessageBox.Show(text, title, MessageBoxButton.YesNo, image) == MessageBoxResult.Yes;

    // 12 characters, upper/lower/digits, no look-alikes (0/O, 1/l/I).
    private static string TemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        for (var i = 3; i < chars.Length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    // ─────────────────────────────────────────────
    // Staff
    // ─────────────────────────────────────────────

    public ObservableCollection<StaffMemberDto> Staff { get; } = new();

    // Active staff with a staff record, for assigning estates.
    public ObservableCollection<StaffMemberDto> AssignableStaff { get; } = new();

    public IReadOnlyList<string> StaffRoles { get; } = new[] { "EstateController", "FirmAdministrator" };

    private string _newStaffName = string.Empty;
    public string NewStaffName
    {
        get => _newStaffName;
        set => SetProperty(ref _newStaffName, value);
    }

    private string _newStaffEmail = string.Empty;
    public string NewStaffEmail
    {
        get => _newStaffEmail;
        set => SetProperty(ref _newStaffEmail, value);
    }

    private string _newStaffRole = "EstateController";
    public string NewStaffRole
    {
        get => _newStaffRole;
        set => SetProperty(ref _newStaffRole, value);
    }

    private string _newStaffPassword = TemporaryPassword();
    public string NewStaffPassword
    {
        get => _newStaffPassword;
        set => SetProperty(ref _newStaffPassword, value);
    }

    public ICommand GeneratePasswordCommand { get; }
    public ICommand CreateStaffCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand UnlockCommand { get; }
    public ICommand DeactivateCommand { get; }
    public ICommand ReactivateCommand { get; }
    public ICommand ResetTwoFactorCommand { get; }

    private async Task LoadStaffAsync()
    {
        var staff = await _apiClient.GetStaffAsync();

        Staff.Clear();
        AssignableStaff.Clear();

        foreach (var member in staff)
        {
            Staff.Add(member);
            if (member.ControllerId.HasValue && !member.IsDeactivated)
                AssignableStaff.Add(member);
        }
    }

    private async Task CreateStaffAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStaffName) || string.IsNullOrWhiteSpace(NewStaffEmail))
        {
            Message = "Enter the staff member's full name and work email address.";
            return;
        }

        var name = NewStaffName.Trim();
        var password = NewStaffPassword;

        await Run(async () =>
        {
            await _apiClient.CreateStaffAsync(new RegisterRequest
            {
                FullName = name,
                Email = NewStaffEmail.Trim(),
                Password = password,
                Role = NewStaffRole
            });

            IssuedPassword = password;
            NewStaffName = string.Empty;
            NewStaffEmail = string.Empty;
            NewStaffRole = "EstateController";
            NewStaffPassword = TemporaryPassword();

            await LoadStaffAsync();
        }, $"Account created for {name}. Give them the temporary password below in person or by phone (not by email), and ask them to set up two-factor authentication after signing in.");
    }

    private async Task StaffActionAsync(object? parameter, string action)
    {
        if (parameter is not StaffMemberDto member)
            return;

        var (question, image) = action switch
        {
            "reset-password" => ($"Give {member.FullName} a new temporary password?", MessageBoxImage.Question),
            "unlock" => ($"Unlock {member.FullName}'s account?", MessageBoxImage.Question),
            "deactivate" => ($"Deactivate {member.FullName}?\n\nThey will be signed out immediately and cannot sign in again. " +
                             "Remember to reassign their estates on the Estates tab.", MessageBoxImage.Warning),
            "reactivate" => ($"Reactivate {member.FullName}'s account?", MessageBoxImage.Question),
            "reset-2fa" => ($"Reset two-factor authentication for {member.FullName}?\n\n" +
                            "Use this when they have lost or replaced their phone. They should set it up again straight after signing in.",
                            MessageBoxImage.Warning),
            _ => (string.Empty, MessageBoxImage.None)
        };

        if (!Confirm(question, "Staff Account", image))
            return;

        var password = action == "reset-password" ? TemporaryPassword() : null;

        await Run(async () =>
        {
            Message = await _apiClient.AdminStaffActionAsync(
                member.UserId,
                action,
                password is null ? null : new ResetPasswordRequest { NewPassword = password });

            if (password is not null)
                IssuedPassword = password;

            await LoadStaffAsync();
        }, null);
    }

    // ─────────────────────────────────────────────
    // Estates (assignment)
    // ─────────────────────────────────────────────

    private List<AdminEstateDto> _allEstates = new();

    public ObservableCollection<AdminEstateDto> Estates { get; } = new();

    private string _estateSearch = string.Empty;
    public string EstateSearch
    {
        get => _estateSearch;
        set
        {
            if (SetProperty(ref _estateSearch, value))
                FilterEstates();
        }
    }

    private AdminEstateDto? _selectedEstate;
    public AdminEstateDto? SelectedEstate
    {
        get => _selectedEstate;
        set => SetProperty(ref _selectedEstate, value);
    }

    private StaffMemberDto? _assignTo;
    public StaffMemberDto? AssignTo
    {
        get => _assignTo;
        set => SetProperty(ref _assignTo, value);
    }

    private StaffMemberDto? _moveFrom;
    public StaffMemberDto? MoveFrom
    {
        get => _moveFrom;
        set => SetProperty(ref _moveFrom, value);
    }

    private StaffMemberDto? _moveTo;
    public StaffMemberDto? MoveTo
    {
        get => _moveTo;
        set => SetProperty(ref _moveTo, value);
    }

    public ICommand AssignSelectedEstateCommand { get; }
    public ICommand MoveAllEstatesCommand { get; }

    private async Task LoadEstatesAsync()
    {
        _allEstates = await _apiClient.GetAllEstatesAsync();
        FilterEstates();
    }

    private void FilterEstates()
    {
        var search = EstateSearch.Trim();

        Estates.Clear();
        foreach (var estate in _allEstates.Where(e =>
                     search.Length == 0 ||
                     e.FileNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     (e.DeceasedFullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (e.ControllerName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            Estates.Add(estate);
        }
    }

    private async Task AssignSelectedEstateAsync()
    {
        if (SelectedEstate is null || AssignTo?.ControllerId is not Guid controllerId)
        {
            Message = "Select an estate and the staff member to assign it to.";
            return;
        }

        var estate = SelectedEstate;

        await Run(async () =>
        {
            Message = await _apiClient.ReassignEstateAsync(estate.EstateId, controllerId);
            await LoadEstatesAsync();
            await LoadStaffAsync();
        }, null);
    }

    // When a controller leaves or goes on extended leave.
    private async Task MoveAllEstatesAsync()
    {
        if (MoveFrom?.ControllerId is not Guid fromId || MoveTo?.ControllerId is not Guid toId || fromId == toId)
        {
            Message = "Choose two different staff members to move estates between.";
            return;
        }

        var estates = _allEstates.Where(e => e.ControllerId == fromId).ToList();
        if (estates.Count == 0)
        {
            Message = $"{MoveFrom.FullName} has no estates assigned.";
            return;
        }

        if (!Confirm($"Move all {estates.Count} estates from {MoveFrom.FullName} to {MoveTo.FullName}?", "Move Estates"))
            return;

        var toName = MoveTo.FullName;

        await Run(async () =>
        {
            foreach (var estate in estates)
                await _apiClient.ReassignEstateAsync(estate.EstateId, toId);

            await LoadEstatesAsync();
            await LoadStaffAsync();
        }, $"{estates.Count} estates moved to {toName}.");
    }

    // ─────────────────────────────────────────────
    // Audit log
    // ─────────────────────────────────────────────

    public ObservableCollection<AuditEntryDto> AuditEntries { get; } = new();

    private string _auditUser = string.Empty;
    public string AuditUser
    {
        get => _auditUser;
        set => SetProperty(ref _auditUser, value);
    }

    private AdminEstateDto? _auditEstate;
    public AdminEstateDto? AuditEstate
    {
        get => _auditEstate;
        set => SetProperty(ref _auditEstate, value);
    }

    public ICommand LoadAuditCommand { get; }

    private async Task LoadAuditAsync()
    {
        await Run(async () =>
        {
            var entries = await _apiClient.GetAuditLogAsync(AuditEstate?.EstateId, AuditUser, 300);

            AuditEntries.Clear();
            foreach (var entry in entries)
                AuditEntries.Add(entry);
        }, null);
    }

    // ─────────────────────────────────────────────
    // Letter templates
    // ─────────────────────────────────────────────

    public ObservableCollection<LetterTemplateDto> Templates { get; } = new();

    private LetterTemplateDto? _selectedTemplate;
    public LetterTemplateDto? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
            {
                TemplateSubject = value?.Subject ?? string.Empty;
                TemplateBody = value?.Body ?? string.Empty;
                OnPropertyChanged(nameof(TemplatePlaceholders));
            }
        }
    }

    public string TemplatePlaceholders =>
        SelectedTemplate is null ? string.Empty : string.Join("   ", SelectedTemplate.Placeholders);

    private string _templateSubject = string.Empty;
    public string TemplateSubject
    {
        get => _templateSubject;
        set => SetProperty(ref _templateSubject, value);
    }

    private string _templateBody = string.Empty;
    public string TemplateBody
    {
        get => _templateBody;
        set => SetProperty(ref _templateBody, value);
    }

    public ICommand SaveTemplateCommand { get; }
    public ICommand ResetTemplateCommand { get; }

    private async Task LoadTemplatesAsync()
    {
        var selected = SelectedTemplate?.LetterType;
        var templates = await _apiClient.GetLetterTemplatesAsync();

        Templates.Clear();
        foreach (var template in templates)
            Templates.Add(template);

        SelectedTemplate = Templates.FirstOrDefault(t => t.LetterType == selected) ?? Templates.FirstOrDefault();
    }

    private async Task SaveTemplateAsync()
    {
        if (SelectedTemplate is null)
            return;

        var type = SelectedTemplate.LetterType;

        await Run(async () =>
        {
            await _apiClient.SaveLetterTemplateAsync(type, new UpdateLetterTemplateRequest
            {
                Subject = TemplateSubject,
                Body = TemplateBody
            });
            await LoadTemplatesAsync();
        }, "Letter wording saved. New letters will use it.");
    }

    private async Task ResetTemplateAsync()
    {
        if (SelectedTemplate is null ||
            !Confirm("Go back to EstateFlow's standard wording for this letter?", "Reset Letter"))
        {
            return;
        }

        var type = SelectedTemplate.LetterType;

        await Run(async () =>
        {
            await _apiClient.ResetLetterTemplateAsync(type);
            await LoadTemplatesAsync();
        }, "Standard wording restored.");
    }

    // ─────────────────────────────────────────────
    // Record retention (POPIA)
    // ─────────────────────────────────────────────

    public ObservableCollection<AdminEstateDto> RetentionCandidates { get; } = new();

    public ICommand LoadRetentionCommand { get; }
    public ICommand PurgeEstateCommand { get; }

    private async Task LoadRetentionAsync()
    {
        await Run(async () =>
        {
            var candidates = await _apiClient.GetRetentionCandidatesAsync();

            RetentionCandidates.Clear();
            foreach (var estate in candidates)
                RetentionCandidates.Add(estate);
        }, null);
    }

    private async Task PurgeAsync(object? parameter)
    {
        if (parameter is not AdminEstateDto estate)
            return;

        if (!Confirm(
                $"Permanently delete estate {estate.FileNumber} ({estate.DeceasedFullName}) and all its documents?\n\n" +
                "This cannot be undone. Make sure the file has been archived if the firm needs to keep a copy.",
                "Purge Estate",
                MessageBoxImage.Warning) ||
            !Confirm($"Are you sure? Estate {estate.FileNumber} will be deleted.", "Confirm Purge", MessageBoxImage.Warning))
        {
            return;
        }

        await Run(async () =>
        {
            Message = await _apiClient.PurgeEstateAsync(estate.EstateId);
            await LoadRetentionAsync();
            await LoadEstatesAsync();
        }, null);
    }
}
