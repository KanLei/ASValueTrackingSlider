using System;
using ASValueTrackingSlider.ValueTrackingSlider;
using Foundation;
using UIKit;

namespace ASValueTrackingSlider
{
    public partial class ValueTrackingSliderViewController : UIViewController, IASValueTrackingSliderDataSource
    {
        public ValueTrackingSliderViewController() : base("ValueTrackingSliderViewController", null)
        {
        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // customize slider 1
            Slider1.MaxValue = 255.0f;
            Slider1.PopUpViewCornerRadius = 0.0f;
            Slider1.SetMaxFractionDigitsDisplayed(0);
            Slider1.PopUpViewColor = UIColor.FromHSBA(0.55f, 0.8f, 0.9f, 0.7f);
            Slider1.Font = UIFont.FromName("GillSans-Bold", 22);
            Slider1.TextColor = UIColor.FromHSBA(0.55f, 1.0f, 0.5f, 1);
            Slider1.PopUpViewWidthPaddingFactor = 3f;


            // customize slider 2
            NSNumberFormatter formatter = new NSNumberFormatter();
            formatter.NumberStyle = NSNumberFormatterStyle.Percent;
            Slider2.NumberFormatter = formatter;
            Slider2.Font = UIFont.FromName("Futura-CondensedExtraBold", 26);
            Slider2.PopUpViewAnimatedColors = new UIColor[] { UIColor.Purple, UIColor.Red, UIColor.Orange };
            Slider2.PopUpViewArrowLength = 20.0f;


            //customize slider 3
            NSNumberFormatter tempFormatter = new NSNumberFormatter();
            tempFormatter.PositiveSuffix = "°C";
            tempFormatter.NegativeSuffix = "°C";

            Slider3.DataSource = this;
            Slider3.NumberFormatter = tempFormatter;
            Slider3.MinValue = -20.0f;
            Slider3.MaxValue = 60.0f;
            Slider3.PopUpViewCornerRadius = 16.0f;

            Slider3.Font = UIFont.FromName("HelveticaNeue-CondensedBlack", 26);
            Slider3.TextColor = UIColor.FromWhiteAlpha(0.0f, 0.5f);

            UIColor coldBlue = UIColor.FromHSBA(0.6f, 0.7f, 1.0f, 1.0f);
            UIColor blue = UIColor.FromHSBA(0.55f, 0.75f, 1.0f, 1.0f);
            UIColor green = UIColor.FromHSBA(0.3f, 0.65f, 0.8f, 1.0f);
            UIColor yellow = UIColor.FromHSBA(0.15f, 0.9f, 0.9f, 1.0f);
            UIColor red = UIColor.FromHSBA(0.0f, 0.8f, 1.0f, 1.0f);

            Slider3.SetPopUpViewAnimatedColors(new UIColor[] { coldBlue, blue, green, yellow, red },
                                               new NSNumber[] { -20, 0, 5, 25, 60 });
        }

        public string Slider(ValueTrackingSlider.ValueTrackingSlider slider, float value)
        {
            nfloat num = NMath.Round(value);

            string s = "";
            if (num < -10.0)
            {
                s = @"❄️Brrr!⛄️";
            }
            else if (num > 29.0 && num < 50.0)
            {
                s = $"😎 { slider.NumberFormatter.StringFromNumber(NSNumber.FromFloat(value)) } 😎";
            }
            else if (num >= 50.0)
            {
                s = @"I’m Melting!";
            }
            return s;
        }
    }
}

