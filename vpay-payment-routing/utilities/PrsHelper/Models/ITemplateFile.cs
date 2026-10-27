// See https://aka.ms/new-console-template for more information
public interface ITemplateFile
{
    string Filename { get; internal set; }
    bool HasChanges { get; internal set; }

    ITemplateFile Save(string filename);

}
