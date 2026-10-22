using Android.App;
using Android.Content;
using Android.Text;
using Android.Text.Style;
using HeatController;
using HeatController.Droid.Controls;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ExtendedPicker), typeof(ExtendedPickerRenderer))]
namespace HeatController.Droid.Controls
{
    public class ExtendedPickerRenderer : PickerRenderer
    {
        private IElementController ElementController => Element as IElementController;
        private AlertDialog _dialog;

        public ExtendedPickerRenderer(Context context) : base(context)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Control.Click -= Control_Click;
            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement == null || e.OldElement != null)
                return;

            Control.Click += Control_Click;
        }

        private void Control_Click(object sender, EventArgs e)
        {
            ExtendedPicker model = Element as ExtendedPicker;
            if (_dialog == null)
            {
                using (var builder = new AlertDialog.Builder(Context))
                {
                    if (!Element.IsSet(Picker.TitleColorProperty))
                    {
                        builder.SetTitle(model.Title ?? "");
                    }
                    else
                    {
                        var title = new SpannableString(model.Title ?? "");
                        title.SetSpan(new ForegroundColorSpan(model.TitleColor.ToAndroid()), 0, title.Length(), SpanTypes.ExclusiveExclusive);

                        builder.SetTitle(title);
                    }

                    string[] items = model.Items.ToArray();
                    builder.SetItems(items, (s, e) => ((IElementController)model).SetValueFromRenderer(Picker.SelectedIndexProperty, e.Which));


                    //builder.SetNegativeButton(global::Android.Resource.String.Cancel, (o, args) => { });
                    builder.SetNegativeButton(model.CancelButton, (o, args) => { });

                    ((IElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, true);

                    _dialog = builder.Create();
                }
                _dialog.SetCanceledOnTouchOutside(true);
                _dialog.DismissEvent += (sender, args) =>
                {
                    (Element as IElementController)?.SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, false);
                    _dialog?.Dispose();
                    _dialog = null;
                };

                _dialog.Show();
            }

        }


    }
}