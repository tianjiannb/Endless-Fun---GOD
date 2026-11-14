using System;

[Serializable]
public struct ChoiceResult
{
    public string paintingId;
    public string choiceId;
    public string outcome;

    public ChoiceResult(string paintingId, string choiceId, string outcome)
    {
        this.paintingId = paintingId;
        this.choiceId = choiceId;
        this.outcome = outcome;
    }

    public bool IsValid
    {
        get { return !string.IsNullOrEmpty(paintingId); }
    }

    public override string ToString()
    {
        return paintingId + "/" + choiceId + " -> " + outcome;
    }
}
