using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPrayerTimes;

internal sealed class ConverterPage : StackPanel
{
	private bool _fromHijri = true;

	private bool _busy;

	private readonly TextBlock _fromLbl = new TextBlock
	{
		FontWeight = FontWeights.SemiBold,
		FontSize = 16.0
	};

	private readonly TextBlock _toLbl = new TextBlock
	{
		FontWeight = FontWeights.SemiBold,
		FontSize = 16.0
	};

	private readonly NumberBox _day = new NumberBox
	{
		Header = L.T("رقم اليوم"),
		Minimum = 1.0,
		Maximum = 31.0,
		SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
		Width = 100.0
	};

	private readonly ComboBox _month = new ComboBox
	{
		Header = L.T("الشهر"),
		HorizontalAlignment = HorizontalAlignment.Stretch
	};

	private readonly NumberBox _year = new NumberBox
	{
		Header = L.T("السنة"),
		SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
		Width = 120.0
	};

	private readonly TextBlock _res = new TextBlock
	{
		FontSize = 26.0,
		FontWeight = FontWeights.SemiBold,
		TextWrapping = TextWrapping.Wrap
	};

	private readonly TextBlock _resDay = HijriUtil.Sec(new TextBlock
	{
		FontSize = 14.0
	});

	public ConverterPage()
	{
		ControlIds.Set(_day, "ConverterDay");
        ControlIds.Set(_month, "ConverterMonth");
        ControlIds.Set(_year, "ConverterYear");
        ControlIds.Set(_fromLbl, "ConverterFrom");
        ControlIds.Set(_res, "ConverterResult");
        base.Spacing = 10.0;
		base.Children.Add(new TextBlock
		{
			Text = L.T("محول التاريخ"),
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold
		});
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0
		};
		stackPanel.Children.Add(_fromLbl);
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(_day, 0);
		Grid.SetColumn(_month, 1);
		Grid.SetColumn(_year, 2);
		grid.Children.Add(_day);
		grid.Children.Add(_month);
		grid.Children.Add(_year);
		stackPanel.Children.Add(grid);
		base.Children.Add(HijriUtil.Card(stackPanel));
		Button button = new Button
		{
			Content = new FontIcon
			{
				Glyph = "\ue8ab"
			},
			HorizontalAlignment = HorizontalAlignment.Center
		};
		ToolTipService.SetToolTip(button, L.T("عكس اتجاه التحويل"));
		ControlIds.Set(button, "ConverterSwap");
        button.Click += delegate
		{
			Swap();
		};
		base.Children.Add(button);
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 4.0
		};
		stackPanel2.Children.Add(_toLbl);
		stackPanel2.Children.Add(_res);
		stackPanel2.Children.Add(_resDay);
		base.Children.Add(HijriUtil.Card(stackPanel2));
		_day.ValueChanged += delegate
		{
			Convert();
		};
		_year.ValueChanged += delegate
		{
			Convert();
		};
		_month.SelectionChanged += delegate
		{
			Convert();
		};
		SetMode(fromHijri: true, DateTime.Today);
	}

	private void SetMode(bool fromHijri, DateTime date)
	{
		_busy = true;
		_fromHijri = fromHijri;
		_fromLbl.Text = (fromHijri ? L.T("من: تاريخ هجري") : L.T("من: تاريخ ميلادي"));
		_toLbl.Text = (fromHijri ? L.T("إلى: تاريخ ميلادي") : L.T("إلى: تاريخ هجري"));
		_month.Items.Clear();
		string[] array = (fromHijri ? HijriUtil.HMonths : HijriUtil.GMonths);
		foreach (string item in array)
		{
			_month.Items.Add(item);
		}
		if (fromHijri)
		{
			var (num, num2, num3) = HijriUtil.ToHijri(date);
            _year.Minimum = double.MinValue;
            _year.Maximum = HijriUtil.MaxYear;
            _year.Minimum = HijriUtil.MinYear;
			_year.Value = num;
			_month.SelectedIndex = num2 - 1;
			_day.Value = num3;
		}
		else
		{
            _year.Maximum = double.MaxValue;
            _year.Minimum = 1901.0;
            _year.Maximum = 2076.0;
			_year.Value = date.Year;
			_month.SelectedIndex = date.Month - 1;
			_day.Value = date.Day;
		}
		_busy = false;
		Convert();
	}

	private DateTime? Current()
	{
		if (double.IsNaN(_day.Value) || double.IsNaN(_year.Value) || _month.SelectedIndex < 0)
		{
			return null;
		}
		int num = (int)_day.Value;
		int num2 = _month.SelectedIndex + 1;
		int num3 = (int)_year.Value;
		try
		{
			if (_fromHijri)
			{
				return HijriUtil.ValidHijri(num3, num2, num) ? new DateTime?(HijriUtil.ToGreg(num3, num2, num)) : ((DateTime?)null);
			}
			if (num3 < 1901 || num3 > 2076 || num > DateTime.DaysInMonth(num3, num2))
			{
				return null;
			}
			return new DateTime(num3, num2, num);
		}
		catch
		{
			return null;
		}
	}

	private void Convert()
	{
		if (_busy)
		{
			return;
		}
		DateTime? dateTime = Current();
		if (!dateTime.HasValue)
		{
			_res.Text = L.T("تاريخ غير صحيح");
			_resDay.Text = "";
			return;
		}
		DateTime value = dateTime.Value;
		if (_fromHijri)
		{
			_res.Text = value.Day + " " + HijriUtil.GMonths[value.Month - 1] + " " + value.Year + L.T(" م");
		}
		else
		{
			var (num, num2, num3) = HijriUtil.ToHijri(value);
			_res.Text = num3 + " " + HijriUtil.HMonths[num2 - 1] + " " + num + L.T(" هـ");
		}
		_resDay.Text = L.T("يوم ") + HijriUtil.Days[(int)value.DayOfWeek];
	}

	private void Swap()
	{
		DateTime date = Current() ?? DateTime.Today;
		SetMode(!_fromHijri, date);
	}
}
