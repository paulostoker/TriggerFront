// _Scripts/Data/FreelancerDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FreelancerDatabase", menuName = "Game/Freelancer Database")]
public class FreelancerDatabase : ScriptableObject
{
    public List<FreelancerData> allFreelancers;
}