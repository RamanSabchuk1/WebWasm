namespace WebWasm.Localization;

public sealed partial class Texts
{
	public MaterialsTexts Materials => field ??= new(Language);
}

/// <summary>Materials page, MaterialsTable, MaterialModal.</summary>
public sealed class MaterialsTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Материалы, которые покупатели заказывают у поставщиков", "Materials buyers order from suppliers");
	public string Add => T("Добавить материал", "Add material");
	public string SearchPlaceholder => T("Поиск по названию и описанию", "Search by name and description");
	public string Empty => T("Материалов пока нет", "No materials yet");
	public string EmptyHint => T("Добавьте первый материал кнопкой «Добавить материал».", "Add the first one with «Add material».");
	public string Solidity => T("Плотность", "Solidity");
	public string Parent => T("Родительский материал", "Parent material");
	public string NoParent => T("Нет (корневой материал)", "None (root material)");
	public string ParentHint => T("Родителем может быть только корневой материал.", "Only a root material can be a parent.");
	public string RootCannotMove => T("Корневой материал нельзя сделать дочерним.", "A root material cannot become a child.");
	public string PhotoUrl => T("Ссылка на фото", "Photo URL");
	public string PhotoUrlPlaceholder => T("https://… (необязательно)", "https://… (optional)");
	public string NamePlaceholder => T("Например, песок мытый", "For example, washed sand");
	public string AddTitle => T("Новый материал", "New material");
	public string EditTitle => T("Изменить материал", "Edit material");
	public string FillRequired => T("Заполните название и описание.", "Fill in the name and the description.");

	public string DeleteTitle => T("Удалить материал?", "Delete the material?");
	public string DeleteMessage(string name) => T($"Материал «{name}» будет удалён. Действие нельзя отменить.", $"«{name}» will be deleted. This cannot be undone.");
	public string DeleteWithChildrenMessage(string name) => T(
		$"Материал «{name}» и все его дочерние материалы будут удалены. Действие нельзя отменить.",
		$"«{name}» and all its child materials will be deleted. This cannot be undone.");
	public string HasChildren => T("Сначала удалите или перенесите дочерние материалы.", "Delete or move the child materials first.");

	public string Created => T("Материал добавлен", "Material added");
	public string Updated => T("Материал сохранён", "Material saved");
	public string DeletedToast => T("Материал удалён", "Material deleted");
	public string SaveFailed(string reason) => T($"Не удалось сохранить материал: {reason}", $"Could not save the material: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить материал: {reason}", $"Could not delete the material: {reason}");
}
