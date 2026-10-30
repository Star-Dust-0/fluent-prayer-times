using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPrayerTimes;

internal sealed class AzkarPage : StackPanel
{
	private readonly Settings _s;

	private readonly Action _changed;

	private readonly TextBlock _text = new TextBlock
	{
		FontSize = 22.0,
		TextWrapping = TextWrapping.Wrap,
		TextAlignment = TextAlignment.Center,
		IsTextSelectionEnabled = true,
		LineHeight = 38.0
	};

	private readonly TextBlock _cat = HijriUtil.Sec(new TextBlock
	{
		FontSize = 12.0,
		HorizontalAlignment = HorizontalAlignment.Center
	});

	private readonly ComboBox _cmb = new ComboBox
	{
		HorizontalAlignment = HorizontalAlignment.Stretch
	};

	private readonly NumberBox _custom = new NumberBox
	{
		Minimum = 1.0,
		Maximum = 1440.0,
		Value = 30.0,
		SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
		MinWidth = 120.0
	};

	private readonly ToggleSwitch _tgl = new ToggleSwitch
	{
		OnContent = L.T("مفع\u0651ل"),
		OffContent = L.T("متوقف")
	};

	private readonly TextBlock _status = HijriUtil.Sec(new TextBlock
	{
		FontSize = 12.0,
		TextWrapping = TextWrapping.Wrap
	});

	private static readonly int[] Presets = new int[5] { 15, 30, 45, 60, 120 };

	private Dhikr _cur = AzkarData.Random();

	private bool _init = true;

	private static Border Card(UIElement child, Thickness? pad = null)
	{
		return HijriUtil.Card(child, pad);
	}

	public AzkarPage(Settings s, Action changed)
	{
		_s = s;
		_changed = changed;
		ControlIds.Set(_tgl, "AzkarToggle");
        ControlIds.Set(_cmb, "AzkarPreset");
        ControlIds.Set(_custom, "AzkarCustom");
        ControlIds.Set(_text, "AzkarText");
        base.Spacing = 10.0;
		base.Children.Add(new TextBlock
		{
			Text = L.T("منبه الأذكار"),
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold
		});
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0
		};
		stackPanel.Children.Add(_text);
		stackPanel.Children.Add(_cat);
		Button button = new Button
		{
			Content = L.T("ذكر آخر"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		ControlIds.Set(button, "AzkarAnother");
        button.Click += delegate
		{
			_cur = AzkarData.Random(_cur);
			Show();
		};
		stackPanel.Children.Add(button);
		base.Children.Add(Card(stackPanel, new Thickness(16.0, 20.0, 16.0, 16.0)));
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 10.0
		};
		stackPanel2.Children.Add(new TextBlock
		{
			Text = L.T("التنبيه بالأذكار"),
			FontSize = 16.0,
			FontWeight = FontWeights.SemiBold
		});
		stackPanel2.Children.Add(_tgl);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = L.T("إرسال تنبيه بالأذكار كل:"),
			FontWeight = FontWeights.SemiBold
		});
		int[] presets = Presets;
		foreach (int num2 in presets)
		{
			_cmb.Items.Add(num2 + L.T(" دقيقة"));
		}
		stackPanel2.Children.Add(_cmb);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = L.T("أو أدخل عدد\u064bا مخصص\u064bا من الدقائق:"),
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		});
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		stackPanel3.Children.Add(_custom);
		Button button2 = new Button
		{
			Content = L.T("تطبيق")
		};
		ControlIds.Set(button2, "AzkarApply");
        button2.Click += delegate
		{
			ApplyCustom();
		};
		stackPanel3.Children.Add(button2);
		stackPanel2.Children.Add(stackPanel3);
		stackPanel2.Children.Add(_status);
		base.Children.Add(Card(stackPanel2));
		base.Children.Add(HijriUtil.Sec(new TextBlock
		{
			Text = "المصدر: كتاب «حصن المسلم» للشيخ سعيد بن علي بن وهف القحطاني، وكتب الأذكار اليومية.",
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4.0, 2.0, 4.0, 0.0)
		}));
		int num3 = Array.IndexOf(Presets, _s.AzkarMinutes);
		_cmb.SelectedIndex = num3;
		if (num3 < 0)
		{
			_custom.Value = _s.AzkarMinutes;
		}
		_tgl.IsOn = _s.AzkarOn;
		_cmb.SelectionChanged += delegate
		{
			if (!_init && _cmb.SelectedIndex >= 0)
			{
				SetMinutes(Presets[_cmb.SelectedIndex]);
			}
		};
		_tgl.Toggled += delegate
		{
			if (!_init)
			{
				_s.AzkarOn = _tgl.IsOn;
				Save();
			}
		};
		_init = false;
		Show();
		UpdateStatus();
	}

	private void Show()
	{
		_text.Text = _cur.Text;
		_cat.Text = _cur.Cat;
	}

	private void ApplyCustom()
	{
		if (!double.IsNaN(_custom.Value))
		{
			int num = (int)Math.Clamp(Math.Round(_custom.Value), 1.0, 1440.0);
			SetMinutes(num);
			_init = true;
			int selectedIndex = Array.IndexOf(Presets, num);
			_cmb.SelectedIndex = selectedIndex;
			_init = false;
		}
	}

	private void SetMinutes(int m)
	{
		_s.AzkarMinutes = m;
		Save();
	}

	private void Save()
	{
		_s.Save();
		_changed();
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		_status.Text = (_s.AzkarOn ? (L.T("التنبيه مفع\u0651ل: كل ") + _s.AzkarMinutes + L.T(" دقيقة.")) : (L.T("التنبيه متوقف. فع\u0651له ليصلك ذكر كل ") + _s.AzkarMinutes + L.T(" دقيقة.")));
	}
}
