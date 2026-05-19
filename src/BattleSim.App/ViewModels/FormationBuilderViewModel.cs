using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media;
using BattleSim.Domain.Models;
using BattleSim.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BattleSim.App.ViewModels;

public sealed partial class FormationBuilderViewModel : ObservableObject
{
    private const int MaxTroops = 5;
    private readonly string templateDirectory;
    private readonly IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions;
    private readonly Action<BattleSide, UnitTemplate>? applyTemplate;
    private readonly Action? templatesSaved;
    private readonly Func<string, IImage?>? getPortraitImage;
    private readonly HashSet<string> existingTemplateIds = new(StringComparer.OrdinalIgnoreCase);
    private FormationBuilderTroopViewModel? draggedTroop;
    private GridPosition? dropPreviewPosition;
    private bool isDropPreviewLegal;
    private int nextSlotNumber = 1;

    public FormationBuilderViewModel(
        string templateDirectory,
        IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions,
        Action<BattleSide, UnitTemplate>? applyTemplate = null,
        Action? templatesSaved = null,
        Func<string, IImage?>? getPortraitImage = null)
    {
        this.templateDirectory = templateDirectory;
        this.classDefinitions = classDefinitions;
        this.applyTemplate = applyTemplate;
        this.templatesSaved = templatesSaved;
        this.getPortraitImage = getPortraitImage;

        AvailableTroopClasses = new ObservableCollection<TroopClassOptionViewModel>(
            classDefinitions.Values
                .OrderBy(definition => definition.DisplayName)
                .Select(definition => new TroopClassOptionViewModel(definition)));
        SelectedTroopClass = AvailableTroopClasses.FirstOrDefault();
        NewTemplate();
    }

    public ObservableCollection<TroopClassOptionViewModel> AvailableTroopClasses { get; }

    public ObservableCollection<FormationBuilderTroopViewModel> DraftTroops { get; } = new();

    public ObservableCollection<FormationBuilderCellViewModel> FormationGridCells { get; } = new();

    public ObservableCollection<string> ValidationMessages { get; } = new();

    [ObservableProperty]
    private string draftTemplateId = string.Empty;

    [ObservableProperty]
    private string draftTemplateName = string.Empty;

    [ObservableProperty]
    private TroopClassOptionViewModel? selectedTroopClass;

    [ObservableProperty]
    private FormationBuilderTroopViewModel? selectedDraftTroop;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isDraggingTroop;

    [ObservableProperty]
    private IImage? draggedPortraitImage;

    [ObservableProperty]
    private double draggedPortraitLeft;

    [ObservableProperty]
    private double draggedPortraitTop;

    public bool CanAddTroop => DraftTroops.Count < MaxTroops && SelectedTroopClass is not null;

    public bool CanSave => GetValidationErrors(includeExistingIdCheck: true).Count == 0;

    public void SetExistingTemplateIds(IEnumerable<string> templateIds)
    {
        existingTemplateIds.Clear();

        foreach (var templateId in templateIds)
        {
            existingTemplateIds.Add(templateId);
        }

        RefreshValidation();
    }

    [RelayCommand]
    public void NewTemplate()
    {
        DraftTemplateId = string.Empty;
        DraftTemplateName = "New Unit";
        DraftTroops.Clear();
        SelectedDraftTroop = null;
        nextSlotNumber = 1;
        StatusMessage = string.Empty;
        RefreshBuilderState();
    }

    [RelayCommand(CanExecute = nameof(CanAddTroop))]
    public void AddTroop()
    {
        if (SelectedTroopClass is null)
        {
            StatusMessage = "Choose a class before adding a troop.";
            return;
        }

        if (DraftTroops.Count >= MaxTroops)
        {
            StatusMessage = "Units can contain at most 5 troops.";
            RefreshValidation();
            return;
        }

        var emptyPosition = FindFirstEmptyPosition();
        if (emptyPosition is null)
        {
            StatusMessage = "No empty formation cells are available.";
            return;
        }

        var slotNumber = nextSlotNumber++;
        var troop = new FormationBuilderTroopViewModel(
            $"{SelectedTroopClass.Id}-{slotNumber}",
            $"{SelectedTroopClass.DisplayName} {slotNumber}",
            SelectedTroopClass.Definition,
            emptyPosition.Value.Row,
            emptyPosition.Value.Column,
            DraftTroops.Count == 0,
            RefreshBuilderState);

        DraftTroops.Add(troop);
        SelectedDraftTroop = troop;
        StatusMessage = $"{troop.Name} added.";
        RefreshBuilderState();
    }

    [RelayCommand]
    public void RemoveSelectedTroop()
    {
        if (SelectedDraftTroop is null)
        {
            StatusMessage = "Select a troop to remove.";
            return;
        }

        var removedTroop = SelectedDraftTroop;
        DraftTroops.Remove(removedTroop);
        SelectedDraftTroop = DraftTroops.FirstOrDefault();
        StatusMessage = $"{removedTroop.Name} removed.";
        RefreshBuilderState();
    }

    [RelayCommand]
    public void SelectOrMoveCell(FormationBuilderCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        var occupyingTroop = DraftTroops.FirstOrDefault(troop =>
            troop.Row == cell.Position.Row &&
            troop.Column == cell.Position.Column);

        if (occupyingTroop is not null)
        {
            SelectedDraftTroop = occupyingTroop;
            StatusMessage = $"{occupyingTroop.Name} selected.";
            RefreshBuilderState();
            return;
        }

        if (SelectedDraftTroop is null)
        {
            StatusMessage = "Select a troop before choosing an empty cell.";
            return;
        }

        MoveSelectedTroop(cell.Position);
    }

    public bool MoveSelectedTroop(GridPosition destination)
    {
        if (SelectedDraftTroop is null)
        {
            StatusMessage = "Select a troop to move.";
            return false;
        }

        if (!destination.IsInFormation)
        {
            StatusMessage = "Troops must stay inside the 3x3 formation.";
            RefreshValidation();
            return false;
        }

        if (DraftTroops.Any(troop =>
            troop != SelectedDraftTroop &&
            troop.Row == destination.Row &&
            troop.Column == destination.Column))
        {
            // Prototype behavior: occupied cells reject moves. Swapping can be added later if it becomes useful.
            StatusMessage = "That cell is already occupied.";
            RefreshValidation();
            return false;
        }

        SelectedDraftTroop.Row = destination.Row;
        SelectedDraftTroop.Column = destination.Column;
        StatusMessage = $"{SelectedDraftTroop.Name} moved.";
        RefreshBuilderState();
        return true;
    }

    public void BeginTroopDrag(FormationBuilderCellViewModel cell, double pointerX, double pointerY)
    {
        var troop = DraftTroops.FirstOrDefault(candidate =>
            candidate.Row == cell.Position.Row &&
            candidate.Column == cell.Position.Column);

        if (troop is null)
        {
            return;
        }

        SelectedDraftTroop = troop;
        draggedTroop = troop;
        IsDraggingTroop = true;
        DraggedPortraitImage = getPortraitImage?.Invoke(troop.ClassDefinition.PortraitAssetPath);
        UpdateDraggedPortraitPosition(pointerX, pointerY);
        RefreshBuilderState();
    }

    public void UpdateTroopDrag(double pointerX, double pointerY, GridPosition? hoverPosition)
    {
        if (!IsDraggingTroop)
        {
            return;
        }

        UpdateDraggedPortraitPosition(pointerX, pointerY);
        dropPreviewPosition = hoverPosition;
        isDropPreviewLegal = hoverPosition.HasValue && IsLegalDropTarget(hoverPosition.Value);
        RefreshGrid();
    }

    public void CompleteTroopDrag(GridPosition? destination)
    {
        if (draggedTroop is not null &&
            destination.HasValue &&
            IsLegalDropTarget(destination.Value) &&
            (draggedTroop.Row != destination.Value.Row || draggedTroop.Column != destination.Value.Column))
        {
            MoveSelectedTroop(destination.Value);
        }

        ClearTroopDrag();
        RefreshBuilderState();
    }

    public void CancelTroopDrag()
    {
        ClearTroopDrag();
        RefreshBuilderState();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public void SaveTemplate()
    {
        var template = BuildDraftTemplate(updateGeneratedId: true);
        var repository = new UnitTemplateRepository(templateDirectory, classDefinitions);

        try
        {
            repository.SaveNew(template);
            existingTemplateIds.Add(template.Id);
            StatusMessage = $"Saved {template.Name}.";
            templatesSaved?.Invoke();
        }
        catch (Exception exception) when (exception is IOException or UnitTemplateValidationException)
        {
            StatusMessage = exception.Message;
        }

        RefreshBuilderState();
    }

    [RelayCommand]
    public void ApplyToBlue()
    {
        ApplyToSide(BattleSide.Left);
    }

    [RelayCommand]
    public void ApplyToRed()
    {
        ApplyToSide(BattleSide.Right);
    }

    public UnitTemplate BuildDraftTemplate(bool updateGeneratedId = false)
    {
        var templateId = DraftTemplateId.Trim();
        if (string.IsNullOrWhiteSpace(templateId) && !string.IsNullOrWhiteSpace(DraftTemplateName))
        {
            templateId = GenerateSafeId(DraftTemplateName);
            if (updateGeneratedId)
            {
                DraftTemplateId = templateId;
            }
        }

        return new UnitTemplate
        {
            Id = templateId,
            Name = DraftTemplateName.Trim(),
            Troops = DraftTroops
                .Select(troop => new UnitTemplateTroop
                {
                    SlotId = troop.SlotId,
                    Name = troop.Name.Trim(),
                    TroopClassId = troop.TroopClassId,
                    Row = troop.Row,
                    Column = troop.Column,
                    IsLeader = troop.IsLeader
                })
                .ToArray()
        };
    }

    partial void OnDraftTemplateIdChanged(string value) => RefreshBuilderState();

    partial void OnDraftTemplateNameChanged(string value) => RefreshBuilderState();

    partial void OnSelectedTroopClassChanged(TroopClassOptionViewModel? value) => RefreshBuilderState();

    partial void OnSelectedDraftTroopChanged(FormationBuilderTroopViewModel? value) => RefreshGrid();

    private void ApplyToSide(BattleSide side)
    {
        var errors = GetValidationErrors(includeExistingIdCheck: false);
        if (errors.Count > 0)
        {
            ReplaceValidationMessages(errors);
            StatusMessage = "Fix validation errors before applying this template.";
            return;
        }

        var template = BuildDraftTemplate(updateGeneratedId: true);
        applyTemplate?.Invoke(side, template);
        StatusMessage = side == BattleSide.Left
            ? $"Applied {template.Name} to Blue."
            : $"Applied {template.Name} to Red.";
        RefreshBuilderState();
    }

    private void RefreshBuilderState()
    {
        RefreshGrid();
        RefreshValidation();
        AddTroopCommand.NotifyCanExecuteChanged();
        SaveTemplateCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanAddTroop));
        OnPropertyChanged(nameof(CanSave));
    }

    private void RefreshGrid()
    {
        FormationGridCells.Clear();

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                var troop = DraftTroops.FirstOrDefault(candidate =>
                    candidate.Row == row &&
                    candidate.Column == column);
                var isDraggedSource = IsDraggingTroop && draggedTroop is not null && troop == draggedTroop;

                if (isDraggedSource)
                {
                    troop = null;
                }

                var position = new GridPosition(row, column);
                var isDropPreview = dropPreviewPosition == position;

                FormationGridCells.Add(new FormationBuilderCellViewModel(
                    position,
                    troop?.Name ?? string.Empty,
                    troop?.ClassName ?? string.Empty,
                    troop is null ? null : getPortraitImage?.Invoke(troop.ClassDefinition.PortraitAssetPath),
                    troop is not null,
                    troop is not null && troop == SelectedDraftTroop,
                    troop?.IsLeader == true,
                    isDropPreview,
                    isDropPreview && isDropPreviewLegal));
            }
        }
    }

    private void RefreshValidation()
    {
        ReplaceValidationMessages(GetValidationErrors(includeExistingIdCheck: true));
    }

    private void ReplaceValidationMessages(IReadOnlyList<string> messages)
    {
        ValidationMessages.Clear();

        foreach (var message in messages)
        {
            ValidationMessages.Add(message);
        }
    }

    private IReadOnlyList<string> GetValidationErrors(bool includeExistingIdCheck)
    {
        var template = BuildDraftTemplate();
        var errors = new List<string>();

        try
        {
            UnitTemplateValidator.ValidateAndThrow(template, classDefinitions);
        }
        catch (UnitTemplateValidationException exception)
        {
            errors.Add(exception.Message);
        }

        if (includeExistingIdCheck &&
            !string.IsNullOrWhiteSpace(template.Id) &&
            existingTemplateIds.Contains(template.Id))
        {
            errors.Add($"Template id '{template.Id}' already exists.");
        }

        return errors;
    }

    private GridPosition? FindFirstEmptyPosition()
    {
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                if (!DraftTroops.Any(troop => troop.Row == row && troop.Column == column))
                {
                    return new GridPosition(row, column);
                }
            }
        }

        return null;
    }

    private void UpdateDraggedPortraitPosition(double pointerX, double pointerY)
    {
        DraggedPortraitLeft = pointerX - 24;
        DraggedPortraitTop = pointerY - 24;
    }

    private void ClearTroopDrag()
    {
        draggedTroop = null;
        dropPreviewPosition = null;
        isDropPreviewLegal = false;
        IsDraggingTroop = false;
        DraggedPortraitImage = null;
    }

    private bool IsLegalDropTarget(GridPosition position)
    {
        if (draggedTroop is null || !position.IsInFormation)
        {
            return false;
        }

        return !DraftTroops.Any(troop =>
            troop != draggedTroop &&
            troop.Row == position.Row &&
            troop.Column == position.Column);
    }

    private static string GenerateSafeId(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var safeId = Regex.Replace(builder.ToString().Trim('-'), "-{2,}", "-");
        return string.IsNullOrWhiteSpace(safeId) ? "unit-template" : safeId;
    }
}
