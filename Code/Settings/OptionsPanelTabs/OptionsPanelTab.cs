namespace LoadingScreenModRevisited
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using ICities;

    internal abstract class OptionsPanelTab
    {
        /// <summary>
        /// Adds a textfield with no title label.
        /// </summary>
        /// <param name="group">UIHelper group</param>
        /// <param name="text">Initial text</param>
        /// <param name="action">Text changed action</param>
        protected void TextField(UIHelper group, string text, OnTextChanged action)
        {
            try
            {
                // Need to add at least a space as a label, otherwise the field won't create.
                UITextField textField = group.AddTextfield(" ", text, action, null) as UITextField;

                // Increase width.
                textField.width = 710f;

                // Change text scale and font.
                textField.textScale = 0.9f;
                textField.font = UIFonts.Regular;
                textField.padding.top = 7;

                // Find label.
                UIComponent parentPanel = textField.parent;
                UILabel uILabel = parentPanel?.Find<UILabel>("Label");
                if (uILabel != null)
                {
                    // Hide label and reduce containing panel height by label height.
                    float height = uILabel.height;
                    uILabel.height = 0f;
                    uILabel.Hide();
                    parentPanel.height -= height;
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "Exception creating options panel textfield");
            }
        }


        /// <summary>
        /// Adds a helper group with reduced bottom offsets.
        /// </summary>
        /// <param name="parent">Parent helper</param>
        /// <param name="title">Group title</param>
        /// <param name="tooltip">Group label tooltip</param>
        /// <returns></returns>
        protected UIHelper AddGroup(UIHelper parent, string title, string tooltip = null)
        {
            // Add helper.
            UIHelper helper = parent.AddGroup(title) as UIHelper;


            // Add tooltip.
            UIPanel helperPanel = helper.self as UIPanel;
            UIPanel helperParent = helperPanel.parent as UIPanel;
            if (!string.IsNullOrEmpty(tooltip))
            {
                UILabel helperLabel = helperParent?.Find<UILabel>("Label");
                if (helperLabel != null)
                {
                    helperLabel.tooltip = tooltip;
                }
            }

            return helper;
        }
    }
}