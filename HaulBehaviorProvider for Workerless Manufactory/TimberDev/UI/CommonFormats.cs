// Timberborn Utils
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Timberborn.Localization;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Global
namespace IgorZ.TimberDev.UI {

/// <summary>Utility class to format strings for UI.</summary>
public static class CommonFormats {
  const string DaysLocKey = "Time.DaysShort";
  const string HoursLocKey = "Time.HoursShort";
  const string SupplyRemainingLocKey = "IgorZ.TimberCommons.WaterTower.SupplyRemaining";

  /// <summary>Formats a string for the "supply last" case.</summary>
  public static string FormatSupplyLeft(ILoc loc, float hours) {
    return loc.T(SupplyRemainingLocKey, DaysHoursFormat(loc, hours));
  }

  /// <summary>Gives a VERY short form of the "hours amount".</summary>
  public static string DaysHoursFormat(ILoc loc, float hoursAmount) {
      if (hoursAmount < 1f)
        return loc.T(HoursLocKey, hoursAmount.ToString("0.##"));
      if (hoursAmount < 10f)
        return loc.T(HoursLocKey, hoursAmount.ToString("0.#"));
    var days = Mathf.FloorToInt(hoursAmount / 24f);
    var hours = Mathf.RoundToInt(hoursAmount % 24f);
    if (days == 0) {
      return loc.T(HoursLocKey, hours.ToString());
    }
    if (hours == 0) {
      return loc.T(DaysLocKey, days.ToString());
    }
    var daysStr = loc.T(DaysLocKey, days.ToString());
    var hoursStr = loc.T(HoursLocKey, hours.ToString());
    return daysStr + " " + hoursStr;
  }

  /// <summary>Formats a value to keep it compact, but still representing small values.</summary>
  /// <param name="value">The value to format.</param>
  public static string FormatSmallValue(float value) {
            if (value >= 10f)
            {
                return value.ToString("F0");
            }

            if (value >= 1f)
            {
                return value.ToString("0.#");
            }

            if (value >= 0.1f)
            {
                return value.ToString("0.0#");
            }

            return value.ToString("0.00#");
  }
}

}
