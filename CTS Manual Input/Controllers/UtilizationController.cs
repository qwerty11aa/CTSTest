﻿using CTS_Manual_Input.Attributes;
using CTS_Manual_Input.Helpers;
using CTS_Manual_Input.Models;
using CTS_Manual_Input.Models.Common;
using CTS_Models;
using CTS_Models.DBContext;
using PagedList;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace CTS_Manual_Input.Controllers
{
	[ErrorAttribute]
	public class UtilizationController : Controller
	{
		private CtsDbContext _cdb;

		public UtilizationController()
		{
			_cdb = new CtsDbContext();
		}

		public UtilizationController(CtsDbContext cdbcontext)
		{
			if (cdbcontext != null)
				_cdb = cdbcontext;
			else
				_cdb = new CtsDbContext();
		}

		[RockUserAuthorization]
		public ActionResult Index(int page = 1)
		{
			string userName = User.Identity.Name ?? "";
			int pagesize = 20;

			var rockUtils = EquipmentProvider.GetUserAuthorizedEquipment<RockUtil>(_cdb, userName);
			var rockUtilsArray = rockUtils.Select(x => x.ID).ToArray();
			var transfers = _cdb.RockUtilTransfers.Where(t => rockUtilsArray.Contains((int)t.EquipID))
				.Where(d => d.TransferTimeStamp >= DbFunctions.AddDays(System.DateTime.Now, -2));

			@ViewBag.Title = "Данные утилизации породы";

			return View(new RockUtil_Transfer
			{
				RockUtils = rockUtils,
				RockUtilTranfers = transfers.OrderByDescending(t => t.TransferTimeStamp).ToPagedList(page, pagesize),
				CanEdit = UserHelper.CanEditUser(userName),
				CanDelete = UserHelper.CanDeleteUser(userName)
			});
		}

		[CanAddRoleAuthorization]
		[RockUserAuthorization]
		public ActionResult Add(int? rockUtilID, string name)
		{
			if (rockUtilID == null)
			{
				return HttpNotFound();
			}
			string userName = User.Identity.Name ?? "";
			var model = new RockUtilTransfer();
			model.Equip = EquipmentProvider.GetUserAuthorizedEquipment<RockUtil>(_cdb, userName).Where(s => s.ID == rockUtilID).Single();
			model.EquipID = rockUtilID;
			model.ID = "R" + rockUtilID + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			model.LasEditDateTime = DateTime.Now;

			ViewBag.Name = name;
			@ViewBag.Title = "Ввод данных утилизации породы";
			return View("Add", model);
		}

		[HttpPost]
		[CanAddRoleAuthorization]
		[RockUserAuthorization]
		[ValidateAntiForgeryToken]
		public ActionResult Add(RockUtilTransfer model, string name)
		{
			string userName = User.Identity.Name ?? "";

			if (!((model.LotQuantity != null) && ((float)model.LotQuantity > 0)))
			{
				ModelState.AddModelError("LotQuantity", "Некорректный вес - должен быть больше 0");
			}
			if (ModelState.IsValid)
			{
				model.LasEditDateTime = DateTime.Now;
				model.IsValid = false;
				model.Status = 1;
				model.OperatorName = UserHelper.GetOperatorName4DBInsertion(Request.UserHostName, User.Identity.Name);
				_cdb.RockUtilTransfers.Add(model);
				_cdb.SaveChanges();

				return RedirectToAction("Index");
			}

			ViewBag.Name = name;
			@ViewBag.Title = "Ввод данных утилизации породы";
			return View("Add", model);
		}

		[CanEditRoleAuthorization]
		[RockUserAuthorization]
		public ActionResult Edit(string ID)
		{
			if (ID != null)
			{
				@ViewBag.Title = "Редактирование данных утилизации породы";
				return View(_cdb.RockUtilTransfers.Find(ID));
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		[CanEditRoleAuthorization]
		[RockUserAuthorization]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(RockUtilTransfer model)
		{
			if (!((model.LotQuantity != null) && ((float)model.LotQuantity > 0)))
			{
				ModelState.AddModelError("LotQuantity", "Некорректный вес - должен быть больше 0");
			}
			if (ModelState.IsValid)
			{
				RockUtilTransfer transfer = _cdb.RockUtilTransfers.Find(model.ID);
				transfer.IsValid = true;
				transfer.Status = 3;
				transfer.LasEditDateTime = DateTime.Now;
				transfer.OperatorName = UserHelper.GetOperatorName4DBInsertion(Request.UserHostName, User.Identity.Name);
				_cdb.Entry(transfer).State = EntityState.Modified;
				_cdb.SaveChanges();

				model.InheritedFrom = transfer.ID;
				model.ID = "R" + model.EquipID + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
				model.OperatorName = UserHelper.GetOperatorName4DBInsertion(Request.UserHostName, User.Identity.Name);
				string name = Request.UserHostName;
				model.LasEditDateTime = DateTime.Now;
				model.IsValid = false;
				model.Status = 2;
				_cdb.RockUtilTransfers.Add(model);
				_cdb.SaveChanges();

				return RedirectToAction("Index");
			}
			
			@ViewBag.Title = "Редактирование данных утилизации породы";
			return View("Edit", model);
		}

		[CanDeleteRoleAuthorization]
		[RockUserAuthorization]
		public ActionResult Delete(string ID)
		{
			if (ID == null)
			{
				return HttpNotFound();
			}
			var transfer = _cdb.RockUtilTransfers.Find(ID);
			if (transfer != null)
			{
				transfer.IsValid = true;
				transfer.Status = 4;
				transfer.LasEditDateTime = DateTime.Now;
				transfer.OperatorName = UserHelper.GetOperatorName4DBInsertion(Request.UserHostName, User.Identity.Name);
				_cdb.Entry(transfer).State = EntityState.Modified;
				_cdb.SaveChanges();
			}

			return RedirectToAction("Index");
		}
	}
}