package Business.Services;

import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;
import Business.Utils.*;
import Common.Utils.OrderedEntity.OrderedEntityHelper;

import java.util.*;

public class TemplateService implements ITemplateService {
    private final IGlobalDataAccess globalDataAccess;
    private final ITemplateDataAccess templateDataAccess;
    private final ITemplateGroupDataAccess templateGroupDataAccess;
    private final IAccountDataAccess accountDataAccess;

    public TemplateService(IGlobalDataAccess globalDataAccess,
                           ITemplateDataAccess templateDataAccess,
                           ITemplateGroupDataAccess templateGroupDataAccess,
                           IAccountDataAccess accountDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.templateDataAccess = templateDataAccess;
        this.templateGroupDataAccess = templateGroupDataAccess;
        this.accountDataAccess = accountDataAccess;
    }

    public void add(Template entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());
        if (entity.getEntries() == null || entity.getEntries().size() < 2) {
            throw new IllegalArgumentException("Invalid amount of Transaction Entries: amount must be more than 1");
        }

        List<TemplateEntry> entries = createEntries(entity);

        TemplateGroup templateGroup = Guard.checkAndGetEntityById(this.templateGroupDataAccess, entity.getParent().getId());
        Guard.checkEntityWithSameName(this.templateDataAccess, templateGroup.getId(), entity);

        Template addedEntity = new Template();
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.templateDataAccess.getMaxOrder(templateGroup.getId()) + 1);

        addedEntity.getEntries().addAll(entries);

        templateGroup.getChildren().add(addedEntity);
        addedEntity.setParent(templateGroup);
        this.templateDataAccess.add(addedEntity);

        this.globalDataAccess.save();
    }

    public void update(Template entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());
        if (entity.getEntries() == null || entity.getEntries().size() < 2) {
            throw new IllegalArgumentException("Template entity is not correct: number of Entries must be more or equal 2");
        }
        List<TemplateEntry> entries = createEntries(entity);

        Template updatedTemplate = Guard.checkAndGetEntityById(this.templateDataAccess, entity.getId());
        Guard.checkEntityWithSameName(this.templateDataAccess, updatedTemplate.getParent().getId(), entity);

        updatedTemplate.setName(entity.getName());
        updatedTemplate.setDescription(entity.getDescription());
        updatedTemplate.setIsFavorite(entity.getIsFavorite());
        updatedTemplate.getEntries().clear();
        updatedTemplate.getEntries().addAll(entries);

        this.templateDataAccess.update(updatedTemplate);

        this.globalDataAccess.save();
    }

    public void delete(UUID entityId) {
        this.globalDataAccess.load();

        Template deletedTemplate = Guard.checkAndGetEntityById(this.templateDataAccess, entityId);
        this.templateDataAccess.loadParent(deletedTemplate);
        TemplateGroup templateGroup = deletedTemplate.getParent();
        this.templateGroupDataAccess.loadChildren(templateGroup);

        templateGroup.getChildren().remove(deletedTemplate);
        this.templateDataAccess.delete(deletedTemplate);

        OrderedEntityHelper.reorder(templateGroup.getChildren());
        this.templateDataAccess.updateList(templateGroup.getChildren());

        this.globalDataAccess.save();
    }

    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        this.globalDataAccess.load();

        Template template = Guard.checkAndGetEntityById(this.templateDataAccess, entityId);
        if (template.getIsFavorite() != isFavorite) {
            template.setIsFavorite(isFavorite);
            this.templateDataAccess.update(template);
        }

        this.globalDataAccess.save();
    }

    public void setOrder(UUID entityId, int order) {
        this.globalDataAccess.load();

        Template template = Guard.checkAndGetEntityById(this.templateDataAccess, entityId);
        if (template.getOrder() != order) {
            this.templateDataAccess.loadParent(template);
            TemplateGroup group = template.getParent();
            this.templateGroupDataAccess.loadChildren(group);
            OrderedEntityHelper.setOrder(group.getChildren(), template, order);
            this.templateDataAccess.updateList(group.getChildren());
        }

        this.globalDataAccess.save();
    }

    public void moveToAnotherParent(UUID entityId, UUID parentId) {
        this.globalDataAccess.load();

        Template template = Guard.checkAndGetEntityById(this.templateDataAccess, entityId);
        this.templateDataAccess.loadParent(template);
        TemplateGroup fromTemplateGroup = template.getParent();
        TemplateGroup toTemplateGroup = Guard.checkAndGetEntityById(this.templateGroupDataAccess, parentId);

        if (fromTemplateGroup.getId() != toTemplateGroup.getId()) {
            Guard.checkEntityWithSameName(this.templateDataAccess, toTemplateGroup.getId(), template);
            this.templateGroupDataAccess.loadChildren(fromTemplateGroup);

            toTemplateGroup.getChildren().add(template);
            template.setParent(toTemplateGroup);
            template.setOrder(this.templateDataAccess.getMaxOrder(fromTemplateGroup.getId()) + 1);

            fromTemplateGroup.getChildren().remove(template);
            this.templateDataAccess.update(template);
            OrderedEntityHelper.reorder(fromTemplateGroup.getChildren());
            this.templateDataAccess.updateList(fromTemplateGroup.getChildren());
        }

        this.globalDataAccess.save();
    }

    private ArrayList<TemplateEntry> createEntries(Template entity) {
        ArrayList<TemplateEntry> entries = new ArrayList<TemplateEntry>();
        for (TemplateEntry entry : entity.getEntries()) {
            Guard.checkInputForNull(entry.getAccount());
            TemplateEntry templateEntry = new TemplateEntry();

            templateEntry.setId(UUID.randomUUID());
            templateEntry.setAccount(Guard.checkAndGetEntityById(this.accountDataAccess, entry.getAccount().getId()));
            templateEntry.setAmount(entry.getAmount());
            ;
            entries.add(templateEntry);
        }

        return entries;
    }
}
