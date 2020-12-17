interface IUsable {
  void Use(Actor a);
}

interface IEdible {
  void Eat(Actor a);
}

interface IPlantable {
  void Plant(Actor a, Soil soil);
}