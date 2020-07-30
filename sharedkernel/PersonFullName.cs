
using System;
using System.Collections.Generic;

namespace SharedKernel {
  public class PersonFullName {

    public static PersonFullName Create (string first, string last) {
      return new PersonFullName (first, last);
    }
    public static PersonFullName Empty () {
      return new PersonFullName (null, null);
    }
    private PersonFullName () { }

    public bool IsEmpty () {
      if (string.IsNullOrEmpty (First) && string.IsNullOrEmpty (Last)) {
        return true;
      } else {
        return false;
      }
    }

    public override bool Equals(object obj)
    {
      return obj is PersonFullName name &&
             base.Equals(obj) &&
             First == name.First &&
             Last == name.Last;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(base.GetHashCode(), First, Last);
    }

    private PersonFullName (string first, string last) {
      First = first;
      Last = last;
    }

    public string First { get; private set; }
    public string Last { get; private set; }
    public string FullName  => First + " " + Last;

    public static bool operator ==(PersonFullName left, PersonFullName right)
    {
      return EqualityComparer<PersonFullName>.Default.Equals(left, right);
    }

    public static bool operator !=(PersonFullName left, PersonFullName right)
    {
      return !(left == right);
    }
  }
}