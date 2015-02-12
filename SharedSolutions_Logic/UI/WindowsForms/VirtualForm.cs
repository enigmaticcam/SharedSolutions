using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedSolutions_Logic.UI.WindowsForms {
    public interface IVirtualForm {
        object ObjectParm { get; }
        EventArgs EventParm { get; }
        IFormatLogic AddFormatLogic(Expression<Func<bool>> expression);
        void PerformAction(Action actionToPerform, object objectParm, EventArgs eventParm);
        void PerformFormatLogic();
    }

    public interface IFormatLogic {
        void ActionOnBoolean(Action<bool> action);
        FormatLogic LogicOnFalse(Func<bool> function);
        FormatLogic LogicOnTrue(Func<bool> function);
    }

    public class VirtualForm : IVirtualForm {
        private List<FormatLogic> _formatLogic = new List<FormatLogic>();

        private object _objectParm;
        public object ObjectParm {
            get { return _objectParm; }
        }

        private EventArgs _eventParm;
        public EventArgs EventParm {
            get { return _eventParm; }
        }

        public IFormatLogic AddFormatLogic(Expression<Func<bool>> expression) {
            FormatLogic logic = new FormatLogic(expression.Compile());
            _formatLogic.Add(logic);
            return logic;
        }

        public void PerformFormatLogic() {
            foreach (FormatLogic formatLogic in _formatLogic) {
                formatLogic.PerformFormatLogic(false);
            }
        }

        public void PerformAction(Action actionToPerform, object objectParm, EventArgs eventParm) {
            _objectParm = objectParm;
            _eventParm = eventParm;
            actionToPerform();
        }
    }

    public class FormatLogic : IFormatLogic {
        private Func<bool> _logic;
        private Dictionary<bool, List<FormatLogic>> _logicOnBoolean = new Dictionary<bool, List<FormatLogic>>();
        private List<Action<bool>> _actionOnBoolean = new List<Action<bool>>();

        public void ActionOnBoolean(Action<bool> action) {
            _actionOnBoolean.Add(action);
        }

        public FormatLogic LogicOnFalse(Func<bool> function) {
            FormatLogic logic = new FormatLogic(function);
            _logicOnBoolean[false].Add(logic);
            return logic;
        }

        public FormatLogic LogicOnTrue(Func<bool> function) {
            FormatLogic logic = new FormatLogic(function);
            _logicOnBoolean[true].Add(logic);
            return logic;
        }

        public void PerformFormatLogic(bool falseOverride) {
            bool logicBoolean = false;
            if (!falseOverride) {
                logicBoolean = _logic();
            }
            CallActions(logicBoolean);
            CallLogic(logicBoolean);
        }

        private void CallActions(bool onBoolean) {
            foreach (Action<bool> action in _actionOnBoolean) {
                action(onBoolean);
            }
        }

        private void CallLogic(bool onBoolean) {
            foreach (FormatLogic formatLogic in _logicOnBoolean[onBoolean]) {
                formatLogic.PerformFormatLogic(!onBoolean);
            }
            if (!onBoolean) {
                CallTrueLogicWithFalseOverride();
            }
        }

        private void CallTrueLogicWithFalseOverride() {
            foreach (FormatLogic formatLogic in _logicOnBoolean[true]) {
                formatLogic.PerformFormatLogic(true);
            }
        }

        public FormatLogic(Func<bool> logic) {
            _logic = logic;
            _logicOnBoolean.Add(true, new List<FormatLogic>());
            _logicOnBoolean.Add(false, new List<FormatLogic>());
        }
    }

    public abstract class VirtualFormDecorator : IVirtualForm {
        private IVirtualForm _form;

        public object ObjectParm {
            get { return _form.ObjectParm; }
        }

        public EventArgs EventParm {
            get { return _form.EventParm; }
        }

        public virtual void PerformAction(Action actionToPerform, object objectParm, EventArgs eventParm) {
            _form.PerformAction(actionToPerform, objectParm, eventParm);
        }

        public virtual IFormatLogic AddFormatLogic(Expression<Func<bool>> expression) {
            return _form.AddFormatLogic(expression);
        }

        public virtual void PerformFormatLogic() {
            _form.PerformFormatLogic();
        }

        public VirtualFormDecorator(IVirtualForm form) {
            _form = form;
        }
    }

    public class VirtualFormDecoratorDontCallDuringProcessing : VirtualFormDecorator {
        private IVirtualForm _form;
        private bool _isProcessing;

        public override void PerformAction(Action actionToPerform, object objectParm, EventArgs eventParm) {
            if (!_isProcessing) {
                _isProcessing = true;
                _form.PerformAction(actionToPerform, objectParm, eventParm);
                _isProcessing = false;
            }
        }

        public VirtualFormDecoratorDontCallDuringProcessing(IVirtualForm form)
            : base(form) {
            _form = form;
        }
    }
}
