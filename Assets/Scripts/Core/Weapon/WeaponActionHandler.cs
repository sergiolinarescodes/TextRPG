using TextRPG.Core.ActionExecution;

namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponActionHandler : IActionHandler
    {
        private readonly IWeaponService _weaponService;

        public string ActionId => ActionNames.Weapon;

        public WeaponActionHandler(IActionHandlerContext ctx)
        {
            _weaponService = ctx.WeaponService;
        }

        public void Execute(ActionContext context)
        {
            _weaponService.EquipWeapon(context.Source, context.Word);
        }
    }
}
