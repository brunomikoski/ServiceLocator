using System;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    [Serializable]
    public class DependencySearchPattern
    {
        [SerializeField]
        private string startsWith;
        public string StartsWith => startsWith;


        [SerializeField]
        private char[] closingCharacters;
        public char[] ClosingCharacters => closingCharacters;

        public DependencySearchPattern(string startWith, char[] closing)
        {
            startsWith = startWith;
            closingCharacters = closing;
        }
    }
}
