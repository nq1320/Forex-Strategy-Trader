// Rate of Change Indicator
// Last changed on 2009-05-05
// Part of Forex Strategy Builder & Forex Strategy Trader
// Website http://forexsb.com/
// This code or any part of it cannot be used in other applications without a permission.
// Copyright (c) 2006 - 2009 Miroslav Popov - All rights reserved.

using System;
using System.Drawing;

namespace Forex_Strategy_Trader
{
    /// <summary>
    /// Rate of Change Indicator
    /// </summary>
    public class Rate_of_Change : Indicator
    {
        /// <summary>
        /// Sets the default indicator parameters for the designated slot type
        /// </summary>
        public Rate_of_Change(SlotTypes slotType)
        {
            // General properties
            IndicatorName  = "Rate of Change";
            PossibleSlots  = SlotTypes.OpenFilter | SlotTypes.CloseFilter;
            SeparatedChart = true;

            // Setting up the indicator parameters
            IndParam = new IndicatorParam();
            IndParam.IndicatorName = IndicatorName;
            IndParam.SlotType      = slotType;

            // The ComboBox parameters
            IndParam.ListParam[0].Caption = "Logic";
            IndParam.ListParam[0].ItemList = new string[]
            {
                "The ROC rises",
                "The ROC falls",
                "The ROC is higher than the Level line",
                "The ROC is lower than the Level line",
                "The ROC crosses the Level line upward",
                "The ROC crosses the Level line downward",
                "The ROC changes its direction upward",
                "The ROC changes its direction downward"
            };
            IndParam.ListParam[0].Index = 0;
            IndParam.ListParam[0].Text = IndParam.ListParam[0].ItemList[IndParam.ListParam[0].Index];
            IndParam.ListParam[0].Enabled = true;
            IndParam.ListParam[0].ToolTip = "Logic of application of the indicator.";

            IndParam.ListParam[1].Caption  = "Smoothing method";
            IndParam.ListParam[1].ItemList = Enum.GetNames(typeof(MAMethod));
            IndParam.ListParam[1].Index    = (int)MAMethod.Simple;
            IndParam.ListParam[1].Text     = IndParam.ListParam[1].ItemList[IndParam.ListParam[1].Index];
            IndParam.ListParam[1].Enabled  = true;
            IndParam.ListParam[1].ToolTip  = "The Moving Average method used for smoothing the ROC value.";

            IndParam.ListParam[2].Caption  = "Base price";
            IndParam.ListParam[2].ItemList = Enum.GetNames(typeof(BasePrice));
            IndParam.ListParam[2].Index    = (int)BasePrice.Close;
            IndParam.ListParam[2].Text     = IndParam.ListParam[2].ItemList[IndParam.ListParam[2].Index];
            IndParam.ListParam[2].Enabled  = true;
            IndParam.ListParam[2].ToolTip  = "The price the indicator is based on.";

            // The NumericUpDown parameters
            IndParam.NumParam[0].Caption = "Period";
            IndParam.NumParam[0].Value   = 10;
            IndParam.NumParam[0].Min     = 1;
            IndParam.NumParam[0].Max     = 200;
            IndParam.NumParam[0].Enabled = true;
            IndParam.NumParam[0].ToolTip = "The period of ROC.";

            IndParam.NumParam[1].Caption = "Smoothing period";
            IndParam.NumParam[1].Value   = 0;
            IndParam.NumParam[1].Min     = 0;
            IndParam.NumParam[1].Max     = 200;
            IndParam.NumParam[1].Enabled = true;
            IndParam.NumParam[1].ToolTip = "The period of additional smoothing.";

            IndParam.NumParam[2].Caption = "Level";
            IndParam.NumParam[2].Value   = 1;
            IndParam.NumParam[2].Min     = 0;
            IndParam.NumParam[2].Max     = 10;
            IndParam.NumParam[2].Point   = 4;
            IndParam.NumParam[2].Enabled = true;
            IndParam.NumParam[2].ToolTip = "A critical level (for the appropriate logic).";

            // The CheckBox parameters
            IndParam.CheckParam[0].Caption = "Use previous bar value";
            IndParam.CheckParam[0].Checked = PrepareUsePrevBarValueCheckBox(slotType);
            IndParam.CheckParam[0].Enabled = true;
            IndParam.CheckParam[0].ToolTip = "Use the indicator value from the previous bar.";

            return;
        }

        /// <summary>
        /// Calculates the indicator's components
        /// </summary>
        public override void Calculate(SlotTypes slotType)
        {
            // Reading the parameters
            MAMethod  maMethod  = (MAMethod )IndParam.ListParam[1].Index;
            BasePrice basePrice = (BasePrice)IndParam.ListParam[2].Index;
            int    iPeriod = (int)IndParam.NumParam[0].Value;
            int    iSmooth = (int)IndParam.NumParam[1].Value;
            double dLevel  = IndParam.NumParam[2].Value;
            int    iPrvs   = IndParam.CheckParam[0].Checked ? 1 : 0;

            int iFirstBar = iPrvs + iPeriod + iSmooth + 2;
            double[] adROC = new double[Bars];
            double[] adBasePrice = Price(basePrice);

            for (int iBar = iPeriod; iBar < Bars; iBar++)
                adROC[iBar] = adBasePrice[iBar] / adBasePrice[iBar - iPeriod];

            if (iSmooth > 0)
            {
                adROC = MovingAverage(iSmooth, 0, maMethod, adROC);
            }

            // Saving the components
            Component = new IndicatorComp[3];

            Component[0] = new IndicatorComp();
            Component[0].CompName   = "ROC";
            Component[0].DataType   = IndComponentType.IndicatorValue;
            Component[0].ChartType  = IndChartType.Line;
            Component[0].ChartColor = Color.Violet;
            Component[0].FirstBar   = iFirstBar;
            Component[0].Value      = adROC;

            Component[1] = new IndicatorComp();
            Component[1].ChartType = IndChartType.NoChart;
            Component[1].FirstBar  = iFirstBar;
            Component[1].Value     = new double[Bars];

            Component[2] = new IndicatorComp();
            Component[2].ChartType = IndChartType.NoChart;
            Component[2].FirstBar  = iFirstBar;
            Component[2].Value     = new double[Bars];

            // Sets the Component's type
            if (slotType == SlotTypes.OpenFilter)
            {
                Component[1].DataType = IndComponentType.AllowOpenLong;
                Component[1].CompName = "Is long entry allowed";
                Component[2].DataType = IndComponentType.AllowOpenShort;
                Component[2].CompName = "Is short entry allowed";
            }
            else if (slotType == SlotTypes.CloseFilter)
            {
                Component[1].DataType = IndComponentType.ForceCloseLong;
                Component[1].CompName = "Close out long position";
                Component[2].DataType = IndComponentType.ForceCloseShort;
                Component[2].CompName = "Close out short position";
            }

            // Calculation of the logic
            IndicatorLogic indLogic = IndicatorLogic.It_does_not_act_as_a_filter;

            switch (IndParam.ListParam[0].Text)
            {
                case "The ROC rises":
                    indLogic = IndicatorLogic.The_indicator_rises;
                    SpecialValues = new double[1] { 1 };
                    break;

                case "The ROC falls":
                    indLogic = IndicatorLogic.The_indicator_falls;
                    SpecialValues = new double[1] { 1 };
                    break;

                case "The ROC is higher than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_higher_than_the_level_line;
                    SpecialValues = new double[2] { dLevel, 2 - dLevel };
                    break;

                case "The ROC is lower than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_lower_than_the_level_line;
                    SpecialValues = new double[2] { dLevel, 2 - dLevel };
                    break;

                case "The ROC crosses the Level line upward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_upward;
                    SpecialValues = new double[2] { dLevel, 2 - dLevel };
                    break;

                case "The ROC crosses the Level line downward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_downward;
                    SpecialValues = new double[2] { dLevel, 2 - dLevel };
                    break;

                case "The ROC changes its direction upward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_upward;
                    SpecialValues = new double[1] { 1 };
                    break;

                case "The ROC changes its direction downward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_downward;
                    SpecialValues = new double[1] { 1 };
                    break;

                default:
                    break;
            }

            OscillatorLogic(iFirstBar, iPrvs, adROC, dLevel, 2 - dLevel, ref Component[1], ref Component[2], indLogic);

            return;
        }

        /// <summary>
        /// Sets the indicator logic description
        /// </summary>
        public override void SetDescription(SlotTypes slotType)
        {
            double fLevelLong  = IndParam.NumParam[2].Value;
            double fLevelShort = 2 - fLevelLong;

            EntryFilterLongDescription  = "the " + ToString() + " ";
            EntryFilterShortDescription = "the " + ToString() + " ";
            ExitFilterLongDescription   = "the " + ToString() + " ";
            ExitFilterShortDescription  = "the " + ToString() + " ";

            switch (IndParam.ListParam[0].Text)
            {
                case "The ROC rises":
                    EntryFilterLongDescription  += "rises";
                    EntryFilterShortDescription += "falls";
                    ExitFilterLongDescription   += "rises";
                    ExitFilterShortDescription  += "falls";
                    break;

                case "The ROC falls":
                    EntryFilterLongDescription  += "falls";
                    EntryFilterShortDescription += "rises";
                    ExitFilterLongDescription   += "falls";
                    ExitFilterShortDescription  += "rises";
                    break;

                case "The ROC is higher than the Level line":
                    EntryFilterLongDescription  += "is higher than the Level " + fLevelLong;
                    EntryFilterShortDescription += "is lower than the Level "  + fLevelShort;
                    ExitFilterLongDescription   += "is higher than the Level " + fLevelLong;
                    ExitFilterShortDescription  += "is lower than the Level "  + fLevelShort;
                    break;

                case "The ROC is lower than the Level line":
                    EntryFilterLongDescription  += "is lower than the Level "  + fLevelLong;
                    EntryFilterShortDescription += "is higher than the Level " + fLevelShort;
                    ExitFilterLongDescription   += "is lower than the Level "  + fLevelLong;
                    ExitFilterShortDescription  += "is higher than the Level " + fLevelShort;
                    break;

                case "The ROC crosses the Level line upward":
                    EntryFilterLongDescription  += "crosses the Level " + fLevelLong  + " upward";
                    EntryFilterShortDescription += "crosses the Level " + fLevelShort + " downward";
                    ExitFilterLongDescription   += "crosses the Level " + fLevelLong  + " upward";
                    ExitFilterShortDescription  += "crosses the Level " + fLevelShort + " downward";
                    break;

                case "The ROC crosses the Level line downward":
                    EntryFilterLongDescription  += "crosses the Level " + fLevelLong  + " downward";
                    EntryFilterShortDescription += "crosses the Level " + fLevelShort + " upward";
                    ExitFilterLongDescription   += "crosses the Level " + fLevelLong  + " downward";
                    ExitFilterShortDescription  += "crosses the Level " + fLevelShort + " upward";
                    break;

                case "The ROC changes its direction upward":
                    EntryFilterLongDescription  += "changes its direction upward";
                    EntryFilterShortDescription += "changes its direction downward";
                    ExitFilterLongDescription   += "changes its direction upward";
                    ExitFilterShortDescription  += "changes its direction downward";
                    break;

                case "The ROC changes its direction downward":
                    EntryFilterLongDescription  += "changes its direction downward";
                    EntryFilterShortDescription += "changes its direction upward";
                    ExitFilterLongDescription   += "changes its direction downward";
                    ExitFilterShortDescription  += "changes its direction upward";
                    break;

                default:
                    break;
            }

            return;
        }

        /// <summary>
        /// Indicator to string
        /// </summary>
        public override string ToString()
        {
            string sString = IndicatorName +
                (IndParam.CheckParam[0].Checked ? "* (" : " (") +
                IndParam.ListParam[1].Text         + ", " + // Method
                IndParam.ListParam[2].Text         + ", " + // Price
                IndParam.NumParam[0].ValueToString + ", " + // Period
                IndParam.NumParam[1].ValueToString + ")";   // Smoothing

            return sString;
        }
    }
}
