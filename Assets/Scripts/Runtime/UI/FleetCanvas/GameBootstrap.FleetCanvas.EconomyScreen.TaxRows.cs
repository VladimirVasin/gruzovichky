using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void EnsureTaxPolicyRowCapacity(int targetCount)
    {
        if (economyScreenUi?.TaxPoliciesContent == null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        while (economyScreenUi.TaxPolicyRows.Count < targetCount)
        {
            int rowIndex = economyScreenUi.TaxPolicyRows.Count;
            TaxPolicyRowUi row = CreateTaxPolicyRow(economyScreenUi.TaxPoliciesContent, font, rowIndex);
            row.ToggleButton.onClick.AddListener(() => ToggleTaxPolicy(row.PolicyId));
            row.RateMinusButton.onClick.AddListener(() => AdjustTaxPolicyRate(row.PolicyId, -1, checkTutorialGoal: true));
            row.RatePlusButton.onClick.AddListener(() => AdjustTaxPolicyRate(row.PolicyId, 1, checkTutorialGoal: true));
            economyScreenUi.TaxPolicyRows.Add(row);
        }
    }
}
