using ColossalFramework.UI;
using ICities;
using System;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    internal abstract class OptionsPanelTab
    {
        /// <summary>
        /// Adds a plain text label to the specified UI panel.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position)</param>
        /// <param name="yPos">Relative y position</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Label width (-1 (default) for autosize)</param>
        /// <param name="width">Text scale (default 1.0)</param>
        /// <returns>New text label</returns>
        protected UILabel AddLabel(UIComponent parent, float xPos, float yPos, string text, float width = -1f, float textScale = 1.0f)
        {
            // Add label.
            UILabel label = (UILabel)parent.AddUIComponent<UILabel>();

            // Set sizing options.
            if (width > 0f)
            {
                // Fixed width.
                label.autoSize = false;
                label.width = width;
                label.autoHeight = true;
                label.wordWrap = true;
            }
            else
            {
                // Autosize.
                label.autoSize = true;
                label.autoHeight = false;
                label.wordWrap = false;
            }

            // Text.
            label.textScale = textScale;
            label.text = text;

            // Position.
            label.relativePosition = new Vector2(xPos, yPos);

            return label;
        }


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
                textField.width *= 2.8f;

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