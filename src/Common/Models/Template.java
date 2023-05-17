package Common.Models;

import Common.Models.Base.ReferenceChildEntity;

import java.util.ArrayList;

public class Template extends ReferenceChildEntity<TemplateGroup> {

    private final ArrayList<TemplateEntry> entries = new ArrayList<>();

    public ArrayList<TemplateEntry> getEntries() {
        return entries;
    }
}
